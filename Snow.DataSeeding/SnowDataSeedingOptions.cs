namespace Snow.DataSeeding;

/// <summary>
/// 数据播种选项
/// </summary>
public sealed class SnowDataSeedingOptions
{
    /// <summary>
    /// 是否在应用启动完成后自动播种, 默认 true
    /// </summary>
    public bool SeedOnStartup { get; set; } = true;
}
