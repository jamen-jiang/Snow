using Rongia.DependencyInjection;

namespace Rongia.MultiTenancy;

/// <summary>
/// 当前租户访问器默认实现: 进程内保存
/// </summary>
public class CurrentTenantAccessor : ICurrentTenantAccessor, ISingletonDependency
{
    /// <inheritdoc />
    public BasicTenantInfo? Current { get; set; }
}
