using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Snow.DependencyInjection;

namespace Snow.BackgroundJobs;

/// <summary>
/// 默认任务执行器: 还原服务类型与方法、反序列化参数后调用
/// <para>任务内部抛出的异常统一包装成 <see cref="BackgroundJobExecutionException"/>(可重试);
/// 找不到类型、对不上方法、参数反序列化失败这类错误直接抛出(不可重试)。</para>
/// </summary>
[TransientService]
public sealed class BackgroundJobInvoker : IBackgroundJobInvoker
{
    private static readonly BindingFlags MethodFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

    private readonly IServiceProvider _serviceProvider;

    private readonly ILogger<BackgroundJobInvoker> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供器, 用于解析承载方法的服务</param>
    /// <param name="logger">日志</param>
    public BackgroundJobInvoker(IServiceProvider serviceProvider, ILogger<BackgroundJobInvoker>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger ?? NullLogger<BackgroundJobInvoker>.Instance;
    }

    /// <inheritdoc />
    public async Task RunAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobInfo);

        Type serviceType = BackgroundJobTypeName.Resolve(jobInfo.ServiceTypeName)
            ?? throw new InvalidOperationException($"找不到后台任务的服务类型 {jobInfo.ServiceTypeName}。");

        MethodInfo method = ResolveMethod(serviceType, jobInfo);
        object?[] arguments = DeserializeArguments(method, jobInfo, cancellationToken);
        object? instance = method.IsStatic ? null : ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, serviceType);

        try
        {
            object? result = method.Invoke(instance, arguments);

            switch (result)
            {
                case Task task:
                    await task.ConfigureAwait(false);
                    break;
                case ValueTask valueTask:
                    await valueTask.ConfigureAwait(false);
                    break;
            }
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw WrapExecutionException(serviceType, method, exception.InnerException);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 停机打断: 原样抛出, 由 worker 判定为"没跑完, 不算失败"
            throw;
        }
        catch (Exception exception) when (exception is not BackgroundJobExecutionException)
        {
            // 任务自己取消(比如内部超时)也算执行失败, 该重试就重试
            throw WrapExecutionException(serviceType, method, exception);
        }
    }

    /// <summary>
    /// 任务内部的失败视为可重试, 包装后交给 worker 判重试次数
    /// </summary>
    private Exception WrapExecutionException(Type serviceType, MethodInfo method, Exception innerException)
    {
        // 任务自己抛的 BackgroundJobExecutionException 说明它已经表过态, 别再套一层
        if (innerException is BackgroundJobExecutionException)
        {
            return innerException;
        }

        _logger.LogDebug(innerException, "后台任务 {Job} 执行失败。", method.Name);

        return new BackgroundJobExecutionException(
            $"后台任务 {serviceType.FullName}.{method.Name} 执行失败: {innerException.Message}",
            innerException,
            serviceType,
            method.Name);
    }

    private static MethodInfo ResolveMethod(Type serviceType, BackgroundJobInfo jobInfo)
    {
        MethodInfo? method = serviceType
            .GetMethods(MethodFlags)
            .FirstOrDefault(candidate => candidate.Name == jobInfo.MethodName && ParametersMatch(candidate, jobInfo.ParameterTypeNames));

        return method ?? throw new InvalidOperationException(
            $"在 {serviceType.FullName} 上找不到方法 {jobInfo.MethodName}({string.Join(", ", jobInfo.ParameterTypeNames)})。");
    }

    private static bool ParametersMatch(MethodInfo method, IReadOnlyList<string> parameterTypeNames)
    {
        ParameterInfo[] parameters = method.GetParameters();

        if (parameters.Length != parameterTypeNames.Count)
        {
            return false;
        }

        for (int index = 0; index < parameters.Length; index++)
        {
            if (!TypeNameMatches(parameters[index].ParameterType, parameterTypeNames[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TypeNameMatches(Type parameterType, string storedName)
    {
        if (string.IsNullOrWhiteSpace(storedName))
        {
            return true;
        }

        if (string.Equals(BackgroundJobTypeName.GetName(parameterType), storedName, StringComparison.Ordinal))
        {
            return true;
        }

        // 程序集名变过时退化成比全名
        string storedFullName = storedName.Split(',')[0].Trim();

        return string.Equals(parameterType.FullName, storedFullName, StringComparison.Ordinal);
    }

    private static object?[] DeserializeArguments(MethodInfo method, BackgroundJobInfo jobInfo, CancellationToken cancellationToken)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] arguments = new object?[parameters.Length];

        for (int index = 0; index < parameters.Length; index++)
        {
            Type parameterType = parameters[index].ParameterType;

            if (parameterType == typeof(CancellationToken))
            {
                arguments[index] = cancellationToken;
                continue;
            }

            arguments[index] = index < jobInfo.Arguments.Count
                ? BackgroundJobSerializer.Deserialize(jobInfo.Arguments[index], parameterType)
                : null;
        }

        return arguments;
    }
}
