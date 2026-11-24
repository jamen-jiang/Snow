using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;
using Snow.Security.Claims;

namespace Snow.Security;

/// <summary>
/// 安全基础模块: 补齐 Web 宿主所需的上下文访问器与安全日志选项
/// <para>主体访问器本身由 <see cref="HttpContextCurrentPrincipalAccessor"/> 上的生命周期注解登记(替换语义顶掉线程实现),
/// 模块里不再重复登记; 认证方案(不透明令牌 / 会话)与授权包都依赖本模块。</para>
/// <para>与 ABP 的差异: ABP 在 <c>OnRegistered</c> 里自动把实现 <see cref="ISnowClaimsPrincipalContributor"/>
/// 的类型加入工厂选项; Snow 暂无 <c>OnRegistered</c> 钩子与 <c>ITypeFinder</c>, 贡献者需在宿主里显式登记:
/// <c>Configure&lt;SnowClaimsPrincipalFactoryOptions&gt;(options =&gt; options.Contributors.Add&lt;MyContributor&gt;())</c>。</para>
/// </summary>
public sealed class SnowSecurityModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 当前主体以 HttpContext.User 为准: 谁顶替线程实现由注解的替换语义决定, 这里只保证上下文访问器可用
        context.Services.AddHttpContextAccessor();
    }
}
