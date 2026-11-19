namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态里的单条声明(可序列化, 不依赖 System.Security.Claims)
/// </summary>
public sealed class SaClaim
{
    /// <summary>
    /// 声明类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 声明值
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
