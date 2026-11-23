using Snow.Modularity;

namespace Snow.Data;

/// <summary>
/// 数据过滤模块(对应 ABP 的 <c>AbpDataModule</c>)。
/// <para>过滤器实现 <see cref="DataFilter{TFilter}"/> 上的生命周期注解负责登记开放泛型
/// (<c>IDataFilter&lt;&gt;</c> → <c>DataFilter&lt;&gt;</c>); 本模块只作为加载锚点, 让本程序集进入约定扫描范围。</para>
/// </summary>
public sealed class SnowDataModule : SnowModule
{
}
