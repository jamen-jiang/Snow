using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// 作业执行入口: Hangfire 只认表达式里的这个类型, 具体类名/方法名由参数传入
/// <para>这样端口不必为每种任务写一个 Hangfire 作业类</para>
/// </summary>
public class HangFireJobInvoker : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    private readonly SnowHangFireJobTypeRegistry _registry;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">作用域内服务提供者</param>
    /// <param name="registry">作业类型表</param>
    public HangFireJobInvoker(IServiceProvider serviceProvider, SnowHangFireJobTypeRegistry registry)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
    }

    /// <summary>
    /// 解析目标服务并调用指定方法
    /// </summary>
    /// <param name="beanName">目标类名(服务注册名)</param>
    /// <param name="methodName">目标方法名</param>
    public async Task InvokeAsync(string beanName, string methodName)
    {
        Type type = _registry.Resolve(beanName);
        object instance = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, type);
        MethodInfo method = FindMethod(type, methodName, beanName);

        object?[] arguments = method.GetParameters().Length == 0 ? [] : [CancellationToken.None];
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

    private static MethodInfo FindMethod(Type type, string methodName, string beanName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        MethodInfo? method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null)
            ?? type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: [typeof(CancellationToken)], modifiers: null);

        return method ?? throw new InvalidOperationException($"类型 {type.FullName} 上找不到可调用的方法 {methodName}(无参或单个 CancellationToken 参数), 来源 beanName={beanName}。");
    }
}
