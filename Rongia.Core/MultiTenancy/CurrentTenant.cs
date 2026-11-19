using System;
using Rongia.DependencyInjection;

namespace Rongia.MultiTenancy;

/// <summary>
/// 当前租户默认实现: 从 <see cref="ICurrentTenantAccessor"/> 读取
/// </summary>
public class CurrentTenant : ICurrentTenant, ITransientDependency
{
    /// <inheritdoc />
    public virtual bool IsAvailable => Id.HasValue;

    /// <inheritdoc />
    public virtual Guid? Id => _currentTenantAccessor.Current?.TenantId;

    /// <inheritdoc />
    public string? Name => _currentTenantAccessor.Current?.Name;

    private readonly ICurrentTenantAccessor _currentTenantAccessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="currentTenantAccessor">当前租户访问器</param>
    public CurrentTenant(ICurrentTenantAccessor currentTenantAccessor)
    {
        _currentTenantAccessor = currentTenantAccessor;
    }

    /// <inheritdoc />
    public IDisposable Change(Guid? id, string? name = null)
    {
        return SetCurrent(id, name);
    }

    private IDisposable SetCurrent(Guid? tenantId, string? name = null)
    {
        BasicTenantInfo? parentScope = _currentTenantAccessor.Current;
        _currentTenantAccessor.Current = new BasicTenantInfo(tenantId, name);

        return new DisposeAction<ValueTuple<ICurrentTenantAccessor, BasicTenantInfo?>>(static (state) =>
        {
            (ICurrentTenantAccessor currentTenantAccessor, BasicTenantInfo? parentScope) = state;
            currentTenantAccessor.Current = parentScope;
        }, (_currentTenantAccessor, parentScope));
    }
}
