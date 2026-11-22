namespace Snow.Settings;

/// <summary>
/// 设置存储: 由实现方持久化(数据库 / Redis 等), 框架只定义契约
/// <para>键是三元组(设置名, 提供器名, 提供器键): 全局设置用 <c>("名字", "G", null)</c>,
/// 租户设置用 <c>("名字", "T", 租户id)</c> —— 与 ABP 的 <c>ISettingStore.GetOrNullAsync(name, providerName, providerKey)</c> 一致。</para>
/// </summary>
public interface ISettingStore
{
    /// <summary>
    /// 取设置值
    /// </summary>
    /// <param name="name">设置名</param>
    /// <param name="providerName">提供器名, 为空表示不按提供器区分</param>
    /// <param name="providerKey">提供器键, 通常是租户 id 或用户 id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置值, 不存在为 null</returns>
    Task<string?> GetOrNullAsync(string name, string? providerName = null, string? providerKey = null, CancellationToken cancellationToken = default);
}
