using Snow.Modularity;

namespace Snow.Threading;

/// <summary>
/// 线程与异步基础设施模块(对应 ABP 的 <c>AbpThreadingModule</c>): 只作为加载锚点, 让本程序集进入约定扫描范围
/// </summary>
public class SnowThreadingModule : SnowModule
{
}
