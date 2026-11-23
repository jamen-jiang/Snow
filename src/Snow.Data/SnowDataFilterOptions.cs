using System;
using System.Collections.Generic;

namespace Snow.Data;

/// <summary>
/// 数据过滤选项(对齐 ABP 的 <c>AbpDataFilterOptions</c>)
/// </summary>
public class SnowDataFilterOptions
{
    /// <summary>
    /// 各过滤器类型的初始状态, 没配的类型按启用处理
    /// </summary>
    public Dictionary<Type, DataFilterState> DefaultStates { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowDataFilterOptions()
    {
        DefaultStates = [];
    }
}
