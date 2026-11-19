namespace Rongia.Security.SaToken;

/// <summary>
/// 令牌生成风格
/// </summary>
public enum SaTokenStyle
{
    /// <summary>
    /// 32 字节随机数的十六进制小写串(64 位), 默认
    /// </summary>
    Hex = 0,

    /// <summary>
    /// 带连字符的 UUID
    /// </summary>
    Uuid = 1,

    /// <summary>
    /// 无连字符的 UUID
    /// </summary>
    SimpleUuid = 2,

    /// <summary>
    /// 32 位随机字母数字
    /// </summary>
    Random32 = 3,

    /// <summary>
    /// 64 位随机字母数字
    /// </summary>
    Random64 = 4,
}
