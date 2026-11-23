using System;

namespace Snow.Data;

/// <summary>
/// 单个过滤器的状态开关(对齐 ABP 的 <c>IDataFilter&lt;TFilter&gt;</c>)
/// </summary>
/// <typeparam name="TFilter">过滤器类型</typeparam>
public interface IDataFilter<TFilter> where TFilter : class
{
    /// <summary>
    /// 启用过滤器, 返回的 <see cref="IDisposable"/> 释放时恢复原状态
    /// </summary>
    /// <returns>释放即恢复的句柄</returns>
    IDisposable Enable();

    /// <summary>
    /// 关闭过滤器, 返回的 <see cref="IDisposable"/> 释放时恢复原状态
    /// </summary>
    /// <returns>释放即恢复的句柄</returns>
    IDisposable Disable();

    /// <summary>
    /// 过滤器当前是否启用
    /// </summary>
    bool IsEnabled { get; }
}
