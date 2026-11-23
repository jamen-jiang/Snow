using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;

namespace Snow.DataSeeding;

/// <summary>
/// 数据播种器默认实现: 按容器登记顺序执行全部出资者
/// </summary>
[TransientService]
public sealed class DataSeeder : IDataSeeder
{
    private readonly IEnumerable<IDataSeedContributor> _contributors;
    private readonly ILogger<DataSeeder> _logger;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="contributors">全部出资者</param>
    /// <param name="logger">日志</param>
    public DataSeeder(IEnumerable<IDataSeedContributor> contributors, ILogger<DataSeeder> logger)
    {
        _contributors = contributors;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SeedAsync(DataSeedContext? context = null, CancellationToken cancellationToken = default)
    {
        context ??= new DataSeedContext();

        foreach (IDataSeedContributor contributor in _contributors)
        {
            _logger.LogDebug("执行数据播种: {Contributor}", contributor.GetType().FullName);
            await contributor.SeedAsync(context).ConfigureAwait(false);
        }
    }
}
