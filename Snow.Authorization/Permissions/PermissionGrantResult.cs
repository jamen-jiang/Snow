namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限授予结果(三态): 未表态 / 已授予 / 已禁止
/// <para>Undefined 表示该提供器对该权限不表态, 由其他提供器继续判定; Prohibited 具有一票否决权。</para>
/// </summary>
public enum PermissionGrantResult
{
    /// <summary>
    /// 不表态
    /// </summary>
    Undefined,

    /// <summary>
    /// 已授予
    /// </summary>
    Granted,

    /// <summary>
    /// 已禁止(一票否决)
    /// </summary>
    Prohibited
}
