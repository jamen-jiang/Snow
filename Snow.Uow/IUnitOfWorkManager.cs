namespace Snow.Uow;

/// <summary>
/// 工作单元管理器: 负责开启工作单元并暴露当前环境工作单元
/// </summary>
public interface IUnitOfWorkManager
{
    /// <summary>
    /// 当前环境工作单元, 没有则为 null
    /// </summary>
    IUnitOfWork? Current { get; }

    /// <summary>
    /// 开启工作单元: 已有环境工作单元且未要求新建时直接复用
    /// </summary>
    /// <param name="options">选项</param>
    /// <returns>工作单元</returns>
    IUnitOfWork Begin(UnitOfWorkOptions? options = null);
}
