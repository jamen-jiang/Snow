using Snow.DependencyInjection;

namespace Snow.Uow;

/// <summary>
/// 基于 <see cref="AsyncLocal{T}"/> 的环境工作单元访问器: 并发请求之间互不干扰
/// </summary>
[SingletonService]
public sealed class UnitOfWorkAccessor : IUnitOfWorkAccessor
{
    private readonly AsyncLocal<IUnitOfWork?> _current = new();

    /// <inheritdoc />
    public IUnitOfWork? Current => _current.Value;

    /// <inheritdoc />
    public void SetUnitOfWork(IUnitOfWork? unitOfWork)
    {
        _current.Value = unitOfWork;
    }
}
