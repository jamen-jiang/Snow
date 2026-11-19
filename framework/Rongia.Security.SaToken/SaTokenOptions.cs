namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态配置(对应 Sa-Token 的 SaTokenConfig)
/// </summary>
public sealed class SaTokenOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "SaToken";

    /// <summary>
    /// 令牌有效期, 默认 7 天(对齐 Java 版 sa-token.timeout=604800 秒)。
    /// <para>滑动语义: 有效期内每访问一次就从当下重新起算, 持续使用不会掉线; 连续 <see cref="Timeout"/> 不访问才失效。</para>
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// 空闲超时(多久无请求即失效), 默认 <see cref="TimeSpan.Zero"/> 表示不启用
    /// </summary>
    public TimeSpan ActiveTimeout { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// 是否允许多端同时在线; 为 false 时新登录会顶掉该账号已有登录态
    /// </summary>
    public bool IsConcurrent { get; set; } = true;

    /// <summary>
    /// 令牌生成风格
    /// </summary>
    public SaTokenStyle TokenStyle { get; set; } = SaTokenStyle.Uuid;

    /// <summary>
    /// 令牌参数名(请求头名, 同时用作 Cookie 名与查询参数名)
    /// </summary>
    public string TokenName { get; set; } = "Authorization";

    /// <summary>
    /// 令牌前缀, 默认 "Bearer "
    /// </summary>
    public string TokenPrefix { get; set; } = "Bearer ";

    /// <summary>
    /// 是否允许从 Cookie 读取令牌
    /// </summary>
    public bool IsReadCookie { get; set; }

    /// <summary>
    /// 是否允许从查询参数读取令牌(便于调试与下载链接)
    /// </summary>
    public bool IsReadQuery { get; set; }

    /// <summary>
    /// 校验配置
    /// </summary>
    public void Validate()
    {
        if (Timeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:Timeout 必须大于 0。");
        }

        if (ActiveTimeout < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{SectionName}:ActiveTimeout 不能为负数(0 表示不启用空闲超时)。");
        }

        if (string.IsNullOrWhiteSpace(TokenName))
        {
            throw new InvalidOperationException($"{SectionName}:TokenName 不能为空。");
        }

        if (!Enum.IsDefined(TokenStyle))
        {
            throw new InvalidOperationException($"{SectionName}:TokenStyle 取值无效。");
        }
    }
}
