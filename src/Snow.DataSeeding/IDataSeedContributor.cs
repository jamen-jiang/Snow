namespace Snow.DataSeeding;

/// <summary>
/// 数据播种出资者: 一个模块写一个, 负责自己那块种子数据
/// <para>实现类打生命周期注解(通常是 TransientService), 播种器会解析容器里的全部实现按顺序执行。</para>
/// </summary>
public interface IDataSeedContributor
{
    /// <summary>
    /// 执行播种
    /// </summary>
    /// <param name="context">播种上下文</param>
    /// <returns>任务</returns>
    Task SeedAsync(DataSeedContext context);
}
