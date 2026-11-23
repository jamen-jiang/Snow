namespace Snow.Features;

/// <summary>
/// 功能定义提供器: 继承它并在 <see cref="Define"/> 里登记本模块的功能
/// <para>实现类打生命周期注解(通常是 [SingletonService]), 定义管理器会扫描容器里的全部实现。</para>
/// </summary>
public abstract class FeatureDefinitionProvider
{
    /// <summary>
    /// 登记功能定义
    /// </summary>
    /// <param name="context">定义上下文</param>
    public abstract void Define(IFeatureDefinitionContext context);
}
