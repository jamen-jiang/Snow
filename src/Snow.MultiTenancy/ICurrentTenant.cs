namespace Snow.MultiTenancy;

/// <summary>
/// 当前租户(对齐 ABP 的 <c>ICurrentTenant</c>)
/// <para>用 <see cref="Change(Guid?, string?)"/> 在一段代码里切到指定租户, 返回的句柄释放时自动还原。</para>
/// </summary>
public interface ICurrentTenant
{
    /// <summary>
    /// 租户 id, null 表示宿主
    /// </summary>
    Guid? Id { get; }

    /// <summary>
    /// 租户名称
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// 当前是否处于某个租户上下文(即 <see cref="Id"/> 有值)
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 切换到指定租户, 返回的 <see cref="IDisposable"/> 释放时还原
    /// </summary>
    /// <param name="id">租户 id, null 表示宿主</param>
    /// <param name="name">租户名称</param>
    /// <returns>释放即还原的句柄</returns>
    IDisposable Change(Guid? id, string? name = null);
}
