using System;
using System.Collections.Generic;

namespace Snow.Data;

/// <summary>
/// 数据过滤选项: 登记各类过滤器在请求开始时的默认状态
/// </summary>
public class SnowDataFilterOptions
{
    /// <summary>
    /// 过滤器类型 → 默认状态, 未登记的按启用处理
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
