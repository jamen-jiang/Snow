namespace Snow.Auditing;

/// <summary>
/// 具有创建时间的实体契约
/// </summary>
public interface IHasCreationTime
{
    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime CreationTime { get; set; }
}
