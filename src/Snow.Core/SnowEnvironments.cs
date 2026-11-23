namespace Snow;

/// <summary>
/// 环境名常量, 取值与 <c>Microsoft.Extensions.Hosting.Environments</c> 完全相同
/// <para>不直接引用那个类型: 它在 Microsoft.Extensions.Hosting.Abstractions 包里, 而 Snow.Core 是
/// 内核包, 只该依赖 Options / 配置这几个最底层的包。字符串值保持一致, 两边的判断结果因此等价。</para>
/// </summary>
public static class SnowEnvironments
{
    /// <summary>
    /// 开发环境
    /// </summary>
    public const string Development = "Development";

    /// <summary>
    /// 预发布环境
    /// </summary>
    public const string Staging = "Staging";

    /// <summary>
    /// 生产环境
    /// </summary>
    public const string Production = "Production";
}
