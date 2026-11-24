using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Timing;

namespace Snow.BackgroundJobs;

/// <summary>
/// 默认入队实现: 把 lambda 拆成"服务类型 + 方法 + 参数", 序列化后写入 <see cref="IBackgroundJobStore"/>, 由内置 worker 轮询执行
/// </summary>
[TransientService]
public sealed class DefaultBackgroundJobManager : IBackgroundJobManager
{
    private readonly IBackgroundJobStore _store;

    private readonly IClock _clock;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="store">任务存储</param>
    /// <param name="clock">时间提供器</param>
    public DefaultBackgroundJobManager(IBackgroundJobStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <inheritdoc />
    /// <param name="job">作业表达式</param>
    /// <param name="delay">延迟时间, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<string> EnqueueAsync(Expression<Action> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        return EnqueueCoreAsync(job, delay, cancellationToken);
    }

    /// <inheritdoc />
    /// <param name="job">作业表达式</param>
    /// <param name="delay">延迟时间, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<string> EnqueueAsync(Expression<Func<Task>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
    {
        return EnqueueCoreAsync(job, delay, cancellationToken);
    }

    /// <inheritdoc />
    /// <param name="job">作业表达式</param>
    /// <param name="delay">延迟时间, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<string> EnqueueAsync<TService>(Expression<Action<TService>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
        where TService : class
    {
        return EnqueueCoreAsync(job, delay, cancellationToken);
    }

    /// <inheritdoc />
    /// <param name="job">作业表达式</param>
    /// <param name="delay">延迟时间, 可为 null</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task<string> EnqueueAsync<TService>(Expression<Func<TService, Task>> job, TimeSpan? delay = null, CancellationToken cancellationToken = default)
        where TService : class
    {
        return EnqueueCoreAsync(job, delay, cancellationToken);
    }

    private async Task<string> EnqueueCoreAsync(LambdaExpression job, TimeSpan? delay, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        (Type serviceType, MethodInfo method, object?[] arguments) = Resolve(job);
        ParameterInfo[] parameters = method.GetParameters();

        List<string> parameterTypeNames = new(parameters.Length);
        List<string> argumentValues = new(parameters.Length);

        for (int index = 0; index < parameters.Length; index++)
        {
            Type parameterType = parameters[index].ParameterType;
            parameterTypeNames.Add(BackgroundJobTypeName.GetName(parameterType));

            // 取消令牌不落库, 占个空串保持两个列表一一对应
            argumentValues.Add(parameterType == typeof(CancellationToken) ? string.Empty : BackgroundJobSerializer.Serialize(arguments[index], parameterType));
        }

        DateTime now = _clock.UtcNow;
        BackgroundJobInfo jobInfo = new()
        {
            Id = Guid.NewGuid(),
            ServiceTypeName = BackgroundJobTypeName.GetName(serviceType),
            MethodName = method.Name,
            ParameterTypeNames = parameterTypeNames,
            Arguments = argumentValues,
            CreationTime = now,
            NextTryTime = delay.HasValue ? now.Add(delay.Value) : now
        };

        await _store.InsertAsync(jobInfo, cancellationToken).ConfigureAwait(false);

        return jobInfo.Id.ToString();
    }

    /// <summary>
    /// 把表达式拆成可持久化的"服务类型 + 方法 + 参数值"
    /// <para>参数在入队这一刻求值: 之后闭包里的变量再变也不影响已入队的任务。</para>
    /// </summary>
    /// <param name="job">作业表达式</param>
    private static (Type ServiceType, MethodInfo Method, object?[] Arguments) Resolve(LambdaExpression job)
    {
        if (job.Body is not MethodCallExpression call)
        {
            throw new ArgumentException("后台任务必须写成方法调用, 例如 () => service.DoAsync(1) 或 s => s.DoAsync(1)。", nameof(job));
        }

        MethodInfo method = call.Method;

        if (!method.IsPublic)
        {
            throw new ArgumentException($"后台任务方法 {method.Name} 必须是 public。", nameof(job));
        }

        if (method.IsDefined(typeof(ExtensionAttribute), inherit: false))
        {
            throw new ArgumentException($"后台任务不支持扩展方法 {method.Name}, 请包一层普通方法再入队。", nameof(job));
        }

        // async void 没法被等待: 失败会直接吞掉, 也拿不到执行结果
        if (method.ReturnType == typeof(void) && method.IsDefined(typeof(AsyncStateMachineAttribute), inherit: false))
        {
            throw new ArgumentException($"后台任务方法 {method.Name} 不能是 async void, 请改成返回 Task。", nameof(job));
        }

        Type serviceType = ResolveServiceType(call, method);

        ParameterInfo[] parameters = method.GetParameters();
        object?[] arguments = new object?[parameters.Length];

        for (int index = 0; index < parameters.Length; index++)
        {
            // 取消令牌不落库, 执行时由 worker 注入
            if (parameters[index].ParameterType == typeof(CancellationToken))
            {
                continue;
            }

            arguments[index] = EvaluateArgument(call.Arguments[index]);
        }

        return (serviceType, method, arguments);
    }

    private static Type ResolveServiceType(MethodCallExpression call, MethodInfo method)
    {
        // 静态方法没有实例, 服务类型就是声明类型
        if (method.IsStatic)
        {
            return method.DeclaringType ?? throw new ArgumentException($"后台任务方法 {method.Name} 找不到声明类型。", nameof(call));
        }

        // s => s.DoAsync(): 用参数声明的类型(通常是接口), 而不是实例的运行时类型 —— 代理类型还原不回来
        if (call.Object is ParameterExpression parameter)
        {
            return parameter.Type;
        }

        Type? objectType = call.Object?.Type;

        // 闭包里捕获的实例: 编译器生成的闭包类型不能落库, 退回方法声明类型
        return objectType is null || objectType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
            ? method.DeclaringType ?? throw new ArgumentException($"后台任务方法 {method.Name} 找不到声明类型。", nameof(call))
            : objectType;
    }

    private static object? EvaluateArgument(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        return Expression.Lambda(expression).Compile().DynamicInvoke();
    }
}
