using Snow.Modularity;
using Snow.Security.Claims;

namespace Snow.Security;

/// <summary>
/// 安全基础模块: 当前主体契约与安全日志选项
/// <para>与 ABP 的 <c>Volo.Abp.Security</c> 一致, 本包不依赖 ASP.NET Core 共享框架: HTTP 上下文访问器与
/// Web 端主体实现(<c>HttpContextCurrentPrincipalAccessor</c>)都放在 <c>Snow.AspNetCore</c>, 由 Web 模块登记。</para>
/// <para>与 ABP 的差异: ABP 在 <c>OnRegistered</c> 里自动把实现 <see cref="ISnowClaimsPrincipalContributor"/>
/// 的类型加入工厂选项; Snow 暂无 <c>OnRegistered</c> 钩子与 <c>ITypeFinder</c>, 贡献者需在宿主里显式登记:
/// <c>Configure&lt;SnowClaimsPrincipalFactoryOptions&gt;(options =&gt; options.Contributors.Add&lt;MyContributor&gt;())</c>。</para>
/// </summary>
public sealed class SnowSecurityModule : SnowModule
{
}
