namespace Snow.Http;

/// <summary>
/// HTTP 相关的常量(对应 ABP 的 <c>AbpHttpConsts</c>)。
/// </summary>
public static class SnowHttpConsts
{
    /// <summary>
    /// 错误格式响应头: 出现即表示响应体是统一的错误信封
    /// </summary>
    public const string SnowErrorFormat = "_SnowErrorFormat";

    /// <summary>
    /// 租户解析错误的响应头
    /// </summary>
    public const string SnowTenantResolveError = "Snow-Tenant-Resolve-Error";
}
