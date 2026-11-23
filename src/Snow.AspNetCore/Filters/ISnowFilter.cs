using System;

namespace Snow.AspNetCore.Filters;

/// <summary>
/// 框架过滤器的标记接口(对应 ABP 的 <c>IAbpFilter</c>)
/// <para>本身没有任何成员, 只用来回答"这个过滤器是框架装的吗": 控制器标了 <see cref="DisableSnowFeaturesAttribute"/>
/// 且开启 <see cref="DisableSnowFeaturesAttribute.DisableMvcFilters"/> 时, <c>SnowMvcActionDescriptorProvider</c>
/// 只摘实现本接口的过滤器, 宿主自己加的过滤器照旧生效。</para>
/// <para>放在接线包 <c>Snow.AspNetCore</c> 而不是 MVC 包: MVC 包与认证包都要能实现它, 而认证包不引用 MVC 包。</para>
/// </summary>
public interface ISnowFilter
{
}
