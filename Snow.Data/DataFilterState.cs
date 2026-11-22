using System;

namespace Snow.Data;

/// <summary>
/// 单个过滤器的开关状态(对齐 ABP 的 <c>DataFilterState</c>)
/// </summary>
public class DataFilterState
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="isEnabled">是否启用</param>
    public DataFilterState(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 复制一份状态: 异步调用链各自持有, 互不影响
    /// </summary>
    /// <returns>新的状态实例</returns>
    public DataFilterState Clone()
    {
        return new DataFilterState(IsEnabled);
    }
}
