using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 管道过滤器清单: 包把自己的过滤器登记进来, 框架展开管道时按清单逐个回调
/// <para>只有这一条登记通道, 没有阶段枚举, 也没有顺序数字: 位置由过滤器实现的是哪个钩子决定,
/// 同一位置内的先后由登记先后(模块依赖拓扑序)决定。</para>
/// </summary>
public class SnowPipelineOptions
{
    /// <summary>
    /// 是否展开框架默认骨架, 默认 true
    /// <para>置 false 等于 Umbraco 的 <c>WithCustomMiddleware</c>: 框架不挂任何一层中间件, 过滤器也不再执行,
    /// 管道完全由宿主自己写(仍然由框架负责在最后映射端点)。</para>
    /// </summary>
    public bool UseDefaultPipeline { get; set; } = true;

    /// <summary>
    /// 已登记的过滤器, 按登记先后
    /// </summary>
    public IList<ISnowPipelineFilter> PipelineFilters { get; } = new List<ISnowPipelineFilter>();

    /// <summary>
    /// 登记一个管道过滤器
    /// <para>同名重复登记直接抛异常: 同一个包挂两次多半是漏了幂等, 静默接受会让中间件跑两遍。</para>
    /// </summary>
    /// <param name="filter">管道过滤器</param>
    public void AddFilter(ISnowPipelineFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        ISnowPipelineFilter? existing = PipelineFilters.FirstOrDefault(item => string.Equals(item.Name, filter.Name, StringComparison.Ordinal));
        if (existing is not null)
        {
            throw new SnowInitializationException("管道过滤器 " + filter.Name + " 已经登记过, 不能重复登记。");
        }

        PipelineFilters.Add(filter);
    }
}
