namespace Snow.Uow;

/// <summary>
/// 环境工作单元访问器: 以异步流程为边界保存当前工作单元(对齐 ABP 的 <c>IAmbientUnitOfWork</c>)
/// </summary>
public interface IUnitOfWorkAccessor
{
    /// <summary>
    /// 当前工作单元
    /// </summary>
    IUnitOfWork? Current { get; }

    /// <summary>
    /// 设置当前工作单元, 传 null 表示清除
    /// </summary>
    /// <param name="unitOfWork">工作单元</param>
    void SetUnitOfWork(IUnitOfWork? unitOfWork);
}
