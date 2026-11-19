using System;

namespace Rongia.Tracing;

/// <summary>
/// 链路标识提供者: 一次调用链路上的唯一标识, 用于把跨服务日志串起来
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// 取当前链路标识
    /// </summary>
    /// <returns>链路标识, 未设置时为 null</returns>
    string? Get();

    /// <summary>
    /// 临时替换链路标识, 释放后恢复原值
    /// </summary>
    /// <param name="correlationId">新的链路标识</param>
    /// <returns>用于恢复的作用域</returns>
    IDisposable Change(string? correlationId);
}
