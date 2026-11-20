namespace Snow.Uow;

/// <summary>
/// 默认工作单元管理器
/// </summary>
public sealed class SnowUnitOfWorkManager : IUnitOfWorkManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWorkAccessor _accessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="accessor">环境工作单元访问器</param>
    public SnowUnitOfWorkManager(IServiceProvider serviceProvider, IUnitOfWorkAccessor accessor)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <inheritdoc />
    public IUnitOfWork? Current => _accessor.Current;

    /// <inheritdoc />
    public IUnitOfWork Begin(UnitOfWorkOptions? options = null)
    {
        UnitOfWorkOptions effectiveOptions = options ?? new UnitOfWorkOptions();

        if (!effectiveOptions.RequiresNew && _accessor.Current is { IsActive: true } current)
        {
            return current;
        }

        SnowUnitOfWork unitOfWork = new(_serviceProvider, effectiveOptions, _accessor);
        _accessor.SetUnitOfWork(unitOfWork);

        return unitOfWork;
    }
}
