namespace Snow.Guids;

/// <summary>
/// 用于生成标识(Id)
/// </summary>
public interface IGuidGenerator
{
    /// <summary>
    /// 创建一个新的 <see cref="Guid"/>
    /// </summary>
    Guid Create();
}
