using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.Security.Claims;

/// <summary>
/// 声明主体贡献者上下文(对应 ABP 的 <c>AbpClaimsPrincipalContributorContext</c>)
/// </summary>
public class SnowClaimsPrincipalContributorContext
{
    /// <summary>
    /// 正在构建的声明主体
    /// </summary>
    public ClaimsPrincipal ClaimsPrincipal { get; set; }

    /// <summary>
    /// 服务提供者
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="claimsPrincipal">声明主体</param>
    /// <param name="serviceProvider">服务提供者</param>
    public SnowClaimsPrincipalContributorContext(ClaimsPrincipal claimsPrincipal, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ClaimsPrincipal = claimsPrincipal;
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// 从容器解析必需的服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public virtual T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }
}
