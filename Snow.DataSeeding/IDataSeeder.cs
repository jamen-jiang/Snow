namespace Snow.DataSeeding;

/// <summary>
/// 数据播种器: 依次执行全部出资者
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// 执行播种
    /// </summary>
    /// <param name="context">播种上下文, 不传则用宿主上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task SeedAsync(DataSeedContext? context = null, CancellationToken cancellationToken = default);
}
