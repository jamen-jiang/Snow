using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.Uow;

/// <summary>
/// 默认工作单元管理器(对应 ABP 的 <c>UnitOfWorkManager</c>)
/// <para>单例: 每开一个新工作单元就从 <see cref="IServiceScopeFactory"/> 建一个独立 DI 作用域,
/// 工作单元及其作用域内的 Scoped 服务随作用域一起销毁。</para>
/// </summary>
[SingletonService]
public sealed class SnowUnitOfWorkManager : IUnitOfWorkManager
{
    private readonly IUnitOfWorkAccessor _accessor;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="accessor">环境工作单元访问器</param>
    /// <param name="serviceScopeFactory">作用域工厂</param>
    public SnowUnitOfWorkManager(IUnitOfWorkAccessor accessor, IServiceScopeFactory serviceScopeFactory)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    }

    /// <inheritdoc />
    public IUnitOfWork? Current => _accessor.Current;

    /// <inheritdoc />
    public IUnitOfWork Begin(UnitOfWorkOptions? options = null)
    {
        UnitOfWorkOptions effectiveOptions = options ?? new UnitOfWorkOptions();

        if (!effectiveOptions.RequiresNew && _accessor.Current is { IsActive: true } current)
        {
            // 已有外层工作单元: 返回子对象, 提交权留在最外层(对应 ABP 的 ChildUnitOfWork)
            return new SnowChildUnitOfWork(current);
        }

        return CreateNewUnitOfWork(effectiveOptions);
    }

    /// <summary>
    /// 新建一个工作单元: 独立 DI 作用域 + 环境登记 + 释放时销毁作用域
    /// </summary>
    /// <param name="options">选项</param>
    /// <returns>工作单元</returns>
    private IUnitOfWork CreateNewUnitOfWork(UnitOfWorkOptions options)
    {
        IServiceScope scope = _serviceScopeFactory.CreateScope();

        try
        {
            IUnitOfWork? outer = _accessor.Current;

            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.Initialize(options);
            unitOfWork.SetOuter(outer);

            _accessor.SetUnitOfWork(unitOfWork);

            unitOfWork.Disposed += (_, _) =>
            {
                _accessor.SetUnitOfWork(outer);
                scope.Dispose();
            };

            return unitOfWork;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}
