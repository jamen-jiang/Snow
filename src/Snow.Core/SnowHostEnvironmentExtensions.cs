namespace Snow;

/// <summary>
/// <see cref="ISnowHostEnvironment"/> 的判断扩展(对应 ABP 的 <c>AbpHostEnvironmentExtensions</c>):
/// 环境名比较一律忽略大小写
/// </summary>
public static class SnowHostEnvironmentExtensions
{
    /// <summary>
    /// 是否开发环境
    /// </summary>
    /// <param name="hostEnvironment">宿主环境</param>
    /// <returns>环境名是 Development 时为 true</returns>
    public static bool IsDevelopment(this ISnowHostEnvironment hostEnvironment)
    {
        Check.NotNull(hostEnvironment, nameof(hostEnvironment));

        return hostEnvironment.IsEnvironment(SnowEnvironments.Development);
    }

    /// <summary>
    /// 是否预发布环境
    /// </summary>
    /// <param name="hostEnvironment">宿主环境</param>
    /// <returns>环境名是 Staging 时为 true</returns>
    public static bool IsStaging(this ISnowHostEnvironment hostEnvironment)
    {
        Check.NotNull(hostEnvironment, nameof(hostEnvironment));

        return hostEnvironment.IsEnvironment(SnowEnvironments.Staging);
    }

    /// <summary>
    /// 是否生产环境
    /// </summary>
    /// <param name="hostEnvironment">宿主环境</param>
    /// <returns>环境名是 Production 时为 true</returns>
    public static bool IsProduction(this ISnowHostEnvironment hostEnvironment)
    {
        Check.NotNull(hostEnvironment, nameof(hostEnvironment));

        return hostEnvironment.IsEnvironment(SnowEnvironments.Production);
    }

    /// <summary>
    /// 是否指定环境
    /// </summary>
    /// <param name="hostEnvironment">宿主环境</param>
    /// <param name="environmentName">环境名</param>
    /// <returns>环境名相同(忽略大小写)时为 true</returns>
    public static bool IsEnvironment(this ISnowHostEnvironment hostEnvironment, string environmentName)
    {
        Check.NotNull(hostEnvironment, nameof(hostEnvironment));

        return string.Equals(
            hostEnvironment.EnvironmentName,
            environmentName,
            StringComparison.OrdinalIgnoreCase
        );
    }
}
