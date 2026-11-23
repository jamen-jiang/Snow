namespace Snow.Uow;

/// <summary>
/// 工作单元选项: 决定本次工作单元是否开启事务、是否强制新开一个
/// </summary>
public sealed class UnitOfWorkOptions
{
    /// <summary>
    /// 是否开启事务(默认开启)
    /// </summary>
    public bool IsTransactional { get; set; } = true;

    /// <summary>
    /// 是否忽略当前已存在的工作单元, 强制新开一个
    /// </summary>
    public bool RequiresNew { get; set; }
}
