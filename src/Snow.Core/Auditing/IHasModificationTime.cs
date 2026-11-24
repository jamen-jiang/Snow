namespace Snow.Auditing;

/// <summary>
/// 具有修改时间的实体契约
/// </summary>
public interface IHasModificationTime
{
    /// <summary>
    /// 最后修改时间
    /// </summary>
    DateTime? LastModificationTime { get; set; }
}
