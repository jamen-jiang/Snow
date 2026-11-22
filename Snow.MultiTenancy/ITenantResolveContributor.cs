namespace Snow.MultiTenancy;

/// <summary>
/// 租户解析出资者(对齐 ABP 的 <c>ITenantResolveContributor</c>)
/// <para>实现类打生命周期注解, 类名建议以 <c>TenantResolveContributor</c> 结尾以便约定注册。</para>
/// </summary>
public interface ITenantResolveContributor
{
    /// <summary>
    /// 出资者名称, 便于排查
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 尝试解析租户, 有结果时写入上下文并置 Handled
    /// </summary>
    /// <param name="context">解析上下文</param>
    /// <returns>异步任务</returns>
    Task ResolveAsync(ITenantResolveContext context);
}
