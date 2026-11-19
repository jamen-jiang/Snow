using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.Modularity;
using Rongia.Security.Claims;
using Module = Rongia.Modularity.Module;

namespace Rongia.Security;

/// <summary>
/// 安全基础模块: 只负责把当前主体访问器换成 Web 版本
/// <para>对齐 ABP 的 <c>AbpSecurityModule</c> + Web 模块对 <c>ICurrentPrincipalAccessor</c> 的替换;
/// 认证方案(不透明令牌 / 会话)与授权包都依赖本模块。</para>
/// </summary>
public sealed class SecurityModule : Module
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.AddHttpContextAccessor();

        // Web 宿主下当前主体以 HttpContext.User 为准, 覆盖约定注册出的线程实现(ABP 的 Web 模块做同样的 Replace)
        context.Services.Replace(ServiceDescriptor.Singleton<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>());
    }
}
