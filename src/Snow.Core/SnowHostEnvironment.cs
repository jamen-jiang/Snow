namespace Snow;

/// <summary>
/// <see cref="ISnowHostEnvironment"/> 的默认实现(对应 ABP 的 <c>AbpHostEnvironment</c>):
/// 一个可写属性, 由框架在模块装配前登记成单例, Web 宿主随后填值
/// </summary>
public class SnowHostEnvironment : ISnowHostEnvironment
{
    /// <inheritdoc />
    public string? EnvironmentName { get; set; }
}
