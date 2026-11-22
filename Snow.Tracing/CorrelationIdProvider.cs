using System;
using Snow.DependencyInjection;

namespace Snow.Tracing;

/// <summary>
/// 链路标识提供者默认实现: 进程内保存, 支持嵌套替换
/// </summary>
[SingletonService]
public class CorrelationIdProvider : ICorrelationIdProvider
{
    /// <summary>
    /// 当前链路标识
    /// </summary>
    protected string? CorrelationId { get; set; }

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

        return new DisposeAction(() => CorrelationId = parent);
    }
}
