namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 决定由谁来闭合"类型参数与接口位置并非一一对应"的开放式行为与处理器,
/// 例如 <c>Behavior&lt;TRequest, TItem&gt; : IPipelineBehavior&lt;TRequest, List&lt;TItem&gt;&gt;</c>。
/// <para>一一对应的开放式泛型(如 <c>Behavior&lt;TRequest, TResponse&gt;</c>)始终按开放式泛型注册。</para>
/// </summary>
public enum OpenBehaviorClosingStrategy
{
    /// <summary>
    /// 中介器扫描已注册的程序集找出请求类型,为每个匹配的请求注册一个闭式实现。
    /// 扫描范围之外的请求(包括闭式泛型请求类型)拿不到该行为。
    /// </summary>
    CloseForScannedRequests,

    /// <summary>
    /// 中介器只注册开放式泛型,把闭合交给容器在解析每个请求时完成。
    /// 适用于按服务类型匹配来闭合开放式泛型的容器(如 DryIoc、LightInject、Autofac),
    /// 且请求来自未被扫描的程序集的场景。
    /// <para>Microsoft 与 Stashbox 容器按位置闭合开放式泛型:遇到这类行为会跳过或失败,
    /// 并在构建服务提供者时拒绝类型参数个数与服务接口不一致的开放式泛型。</para>
    /// <para>类型参数只出现在约束中的实现,任何容器都无法闭合,因此仍然按已扫描请求闭合。</para>
    /// </summary>
    CloseInContainer
}
