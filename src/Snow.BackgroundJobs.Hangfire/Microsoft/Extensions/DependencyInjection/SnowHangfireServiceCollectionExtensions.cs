using global::Hangfire;
using Snow.BackgroundJobs.Hangfire;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Hangfire 的服务登记入口
/// </summary>
public static class SnowHangfireServiceCollectionExtensions
{
    /// <summary>
    /// 登记 Hangfire, 并套上框架统一的默认设置(数据兼容级别 v1.8 / 简单程序集名类型序列化 / 推荐序列化设置)。
    /// <para>存储由调用方在 <paramref name="configure"/> 里指定, 例如
    /// <c>AddSnowHangfire(configuration =&gt; configuration.UseSqlServerStorage(connectionString))</c>;
    /// 不指定时由 <c>SnowHangfireModule</c> 按 <c>Hangfire:UseInMemoryStorage</c> 退回进程内存储。</para>
    /// <para>与 ABP 一样, 存储提供程序(Hangfire.SqlServer / Hangfire.PostgreSql / Hangfire.Storage.SQLite 等)由宿主自己引, 本包不绑定任何一种库。</para>
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">追加 Hangfire 全局配置, 通常就是指存储</param>
    /// <returns>服务集合本身</returns>
    public static IServiceCollection AddSnowHangfire(this IServiceCollection services, Action<IGlobalConfiguration>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHangfire(configuration =>
        {
            configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            configure?.Invoke(configuration);
        });

        return services;
    }
}
