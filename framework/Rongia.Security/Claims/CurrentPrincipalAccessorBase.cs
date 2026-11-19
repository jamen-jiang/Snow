using System.Security.Claims;

namespace Rongia.Security.Claims;

/// <summary>
/// 主体访问器基类: 用 <see cref="AsyncLocal{T}"/> 保存一次请求内的替换值, 未替换时回落到派生类提供的主体
/// <para>对齐 ABP 的 <c>CurrentPrincipalAccessorBase</c>。</para>
/// </summary>
public abstract class CurrentPrincipalAccessorBase : ICurrentPrincipalAccessor
{
    private readonly AsyncLocal<ClaimsPrincipal?> _currentPrincipal = new();

    /// <inheritdoc />
    public ClaimsPrincipal Principal => _currentPrincipal.Value ?? GetClaimsPrincipal();

    /// <summary>
    /// 取宿主默认主体(如 HttpContext.User 或 Thread.CurrentPrincipal)
    /// </summary>
    /// <returns>默认主体</returns>
    protected abstract ClaimsPrincipal GetClaimsPrincipal();

    /// <inheritdoc />
    public virtual IDisposable Change(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        ClaimsPrincipal? parent = _currentPrincipal.Value;
        _currentPrincipal.Value = principal;

        return new RestoreAction(this, parent);
    }

    /// <summary>
    /// 恢复上一个主体
    /// </summary>
    private sealed class RestoreAction : IDisposable
    {
        private readonly CurrentPrincipalAccessorBase _accessor;
        private readonly ClaimsPrincipal? _parent;
        private bool _disposed;

        public RestoreAction(CurrentPrincipalAccessorBase accessor, ClaimsPrincipal? parent)
        {
            _accessor = accessor;
            _parent = parent;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _accessor._currentPrincipal.Value = _parent;
        }
    }
}
