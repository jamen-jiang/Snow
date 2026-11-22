using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.Modularity;

namespace Snow.DataSeeding;

/// <summary>
/// 数据播种模块(对应 ABP 的 AbpDataModule 中播种那一部分)
/// <para>启动阶段末尾按开关创建独立作用域执行播种, 出资者异常直接抛出, 让启动失败而不是带着半套数据跑。</para>
/// </summary>
public sealed class SnowDataSeedingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Configuration.GetSection("DataSeeding").Exists())
        {
            Configure<SnowDataSeedingOptions>(context.Configuration.GetSection("DataSeeding"));
        }
    }

    /// <inheritdoc />
    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SnowDataSeedingOptions options = context.ServiceProvider.GetRequiredService<IOptions<SnowDataSeedingOptions>>().Value;
        if (!options.SeedOnStartup)
        {
            return;
        }

        using IServiceScope scope = context.ServiceProvider.CreateScope();
        IDataSeeder seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SnowDataSeedingModule).FullName!);

        logger.LogDebug("开始数据播种");
        await seeder.SeedAsync().ConfigureAwait(false);
        logger.LogDebug("数据播种完成");
    }
}
