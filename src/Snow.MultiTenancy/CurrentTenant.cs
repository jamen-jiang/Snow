using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.MultiTenancy;

/// <summary>
/// 当前租户的默认实现(对齐 ABP 的 <c>CurrentTenant</c>)
/// <para>多租户关闭时租户上下文恒为宿主, 切换调用直接忽略。</para>
/// </summary>
[SingletonService]
public sealed class CurrentTenant : ICurrentTenant
{
    private readonly ICurrentTenantAccessor _accessor;

    private readonly IMultiTenancyConfig _config;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="accessor">租户信息容器</param>
    /// <param name="config">多租户开关</param>
    public CurrentTenant(ICurrentTenantAccessor accessor, IMultiTenancyConfig config)
    {
        _accessor = accessor;
        _config = config;
    }

    /// <inheritdoc />
    public Guid? Id => _config.IsEnabled ? _accessor.Current?.TenantId : null;

    /// <inheritdoc />
    public string? Name => _config.IsEnabled ? _accessor.Current?.Name : null;

    /// <inheritdoc />
    public bool IsAvailable => Id.HasValue;

    /// <inheritdoc />
    public IDisposable Change(Guid? id, string? name = null)
    {
        if (!_config.IsEnabled)
        {
            return NullDisposable.Instance;
        }

        TenantInfo? parent = _accessor.Current;
        _accessor.Current = new TenantInfo(id, name);

        return new DisposeAction(() => _accessor.Current = parent);
    }
}
