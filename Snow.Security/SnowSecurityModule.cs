using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;
using Snow.Security.Claims;

namespace Snow.Security;

/// <summary>
/// 安全基础模块: 只负责补齐 Web 宿主的 HTTP 上下文访问器
/// <para>主体访问器本身由 <see cref="HttpContextCurrentPrincipalAccessor"/> 上的生命周期注解登记(替换语义顶掉线程实现),
/// 模块里不再重复登记; 认证方案(不透明令牌 / 会话)与授权包都依赖本模块。</para>
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
