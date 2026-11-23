namespace Snow.Domain.Entities.Auditing;

/// <summary>
/// 删除审计契约: 记录谁在什么时候删除
/// </summary>
public interface IDeletionAudited : ISoftDelete
{
    /// <summary>
    /// 删除人Id
    /// </summary>
    long? DeleterId { get; set; }

    /// <summary>
    /// 删除人名称
    /// </summary>
    string? Deleter { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    DateTime? DeletionTime { get; set; }
}
