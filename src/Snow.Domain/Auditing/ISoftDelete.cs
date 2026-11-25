namespace Snow.Auditing;

/// <summary>
/// 软删除契约: 删除只是标记, 数据仍在库中
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// 是否已删除
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// 假删除
    /// </summary>
    void Delete();
}
