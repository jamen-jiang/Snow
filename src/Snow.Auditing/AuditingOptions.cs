namespace Snow.Auditing;

/// <summary>
/// 审计配置: 绑定配置节 <c>Auditing</c>
/// <para><see cref="IgnoredNamespaces"/> 默认排除框架自身命名空间, 避免把框架内部调用也记成业务审计。</para>
/// </summary>
public sealed class AuditingOptions
{
    /// <summary>
    /// 总开关, 关闭后所有审计拦截器直接放行
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否记录匿名用户的调用
    /// </summary>
    public bool IsEnabledForAnonymousUsers { get; set; } = true;

    /// <summary>
    /// 是否记录 GET 请求(查询通常量太大, 默认关闭)
    /// </summary>
    public bool IsEnabledForGetRequests { get; set; }

    /// <summary>
    /// 是否连框架命名空间里的类型也记录(默认否, 只看 <see cref="IgnoredNamespaces"/>)
    /// </summary>
    public bool AlwaysLogFrameworkTypes { get; set; }

    /// <summary>
    /// 应用名称, 未配置时取入口程序集名称
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// 参数与返回值序列化后的最大字符数, 超出截断
    /// </summary>
    public int MaxSerializedLength { get; set; } = 32_000;

    /// <summary>
    /// 不参与审计的类型
    /// </summary>
    public HashSet<Type> IgnoredTypes { get; } = [];

    /// <summary>
    /// 不参与审计的命名空间前缀
    /// </summary>
    public HashSet<string> IgnoredNamespaces { get; } = new(StringComparer.Ordinal) { "Snow." };
}
