using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Security;
using Snow.Timing;

namespace Snow.Auditing;

/// <summary>
/// 审计模块: 绑定 <c>Auditing</c> 配置节
/// <para>服务本身靠注解登记: <see cref="AuditingAttribute"/> 是拦截器, 其余实现标了生命周期注解。</para>
/// </summary>
[DependsOn(typeof(SnowSecurityModule), typeof(SnowTimingModule))]
public sealed class SnowAuditingModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<AuditingOptions>(context.Services.GetConfiguration().GetSection("Auditing"));
    }
}
