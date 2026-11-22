using Snow.DependencyInjection;

namespace Snow.MultiTenancy;

/// <summary>
/// 当前租户信息的默认容器: 状态存在 <see cref="AsyncLocal{T}"/> 里
/// <para>同一次异步调用链共享, 跨请求隔离; 释放 <see cref="IDisposable"/> 时自动还原。</para>
/// </summary>
[SingletonService]
public sealed class CurrentTenantAccessor : ICurrentTenantAccessor
{
    private readonly AsyncLocal<TenantInfo?> _current = new();

    /// <inheritdoc />
    public TenantInfo? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
