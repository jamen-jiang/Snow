using System.Reflection;

namespace Snow.Aop;

/// <summary>
/// 方法调用上下文: 一次被拦截调用在拦截器之间流转的全部信息。
/// <para>特性形式的拦截器不能构造注入, 需要其他服务时从 <see cref="ServiceProvider"/> 解析。</para>
/// </summary>
public sealed class AopMethodInvocation
{
    private Func<Task> _proceed;
    private IReadOnlyDictionary<string, object?>? _argumentsDictionary;

    /// <summary>
    /// 创建调用上下文
    /// </summary>
    /// <param name="target">被代理的目标对象</param>
    /// <param name="method">被调用的方法</param>
    /// <param name="arguments">实参数组, 可原地修改</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="proceed">推进到下一环的委托</param>
    public AopMethodInvocation(object? target, MethodInfo method, object?[] arguments, IServiceProvider serviceProvider, Func<Task> proceed)
    {
        Target = target;
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _proceed = proceed ?? throw new ArgumentNullException(nameof(proceed));
    }

    /// <summary>
    /// 被代理的目标对象
    /// </summary>
    public object? Target { get; }

    /// <summary>
    /// 被调用的方法
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// 实参数组, 可原地修改
    /// </summary>
    public object?[] Arguments { get; }

    /// <summary>
    /// 按参数名索引实参的只读视图(首次访问时构建)
    /// </summary>
    public IReadOnlyDictionary<string, object?> ArgumentsDictionary => _argumentsDictionary ??= BuildArgumentsDictionary();

    /// <summary>
    /// 目标方法的返回值: 拦截器可读取以包装结果, 也可直接赋值以实现短路或缓存
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// 服务提供者: 特性无法构造注入, 需要其他服务时从这里解析
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 推进到拦截链的下一环; 最内层会真正执行目标方法
    /// </summary>
    /// <returns>表示调用过程的任务</returns>
    public Task ProceedAsync()
    {
        return _proceed();
    }

    /// <summary>
    /// 替换下一环委托(由代理适配器在进入每一环前设置)
    /// </summary>
    /// <param name="proceed">新的下一环委托</param>
    internal void SetProceed(Func<Task> proceed)
    {
        _proceed = proceed ?? throw new ArgumentNullException(nameof(proceed));
    }

    private Dictionary<string, object?> BuildArgumentsDictionary()
    {
        ParameterInfo[] parameters = Method.GetParameters();
        Dictionary<string, object?> dictionary = new(parameters.Length, StringComparer.Ordinal);

        for (int index = 0; index < parameters.Length && index < Arguments.Length; index++)
        {
            dictionary[parameters[index].Name ?? "arg" + index] = Arguments[index];
        }

        return dictionary;
    }
}
