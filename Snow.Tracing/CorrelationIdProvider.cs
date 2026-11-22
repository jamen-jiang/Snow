using System;
using System.Threading;
using Snow.DependencyInjection;

namespace Snow.Tracing;

/// <summary>
/// 链路标识提供者默认实现: 状态存在 <see cref="AsyncLocal{T}"/> 里, 支持嵌套替换
/// <para>同一次异步调用链共享, 跨请求隔离; 释放 <see cref="IDisposable"/> 时自动还原。</para>
/// </summary>
[SingletonService]
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private readonly AsyncLocal<string?> _correlationId = new();

    /// <summary>
    /// 当前链路标识
    /// </summary>
    protected string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    /// <inheritdoc />
    public virtual string? Get()
    {
        return CorrelationId;
    }

    /// <inheritdoc />
    public virtual IDisposable Change(string? correlationId)
    {
        string? parent = Get();
        CorrelationId = correlationId;

        return new RestoreAction(this, parent);
    }

    /// <summary>
    /// 恢复上一个链路标识
    /// </summary>
    private sealed class RestoreAction : IDisposable
    {
        private readonly CorrelationIdProvider _provider;
        private readonly string? _parent;
        private bool _disposed;

        public RestoreAction(CorrelationIdProvider provider, string? parent)
        {
            _provider = provider;
            _parent = parent;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _provider.CorrelationId = _parent;
        }
    }
}
