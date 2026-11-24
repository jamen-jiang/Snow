using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Security.Claims;

/// <summary>
/// 默认声明主体工厂(对应 ABP 的 <c>AbpClaimsPrincipalFactory</c>)
/// </summary>
[TransientService]
public class SnowClaimsPrincipalFactory : ISnowClaimsPrincipalFactory
{
    /// <summary>
    /// 认证类型
    /// </summary>
    public static string AuthenticationType => "Snow.Application";

    /// <summary>
    /// 服务提供者
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 工厂选项
    /// </summary>
    protected SnowClaimsPrincipalFactoryOptions Options { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="options">工厂选项</param>
    public SnowClaimsPrincipalFactory(IServiceProvider serviceProvider, IOptions<SnowClaimsPrincipalFactoryOptions> options)
    {
        ServiceProvider = serviceProvider;
        Options = options.Value;
    }

    /// <inheritdoc />
    /// <param name="existsClaimsPrincipal">已有主体, 可为 null</param>
    public virtual async Task<ClaimsPrincipal> CreateAsync(ClaimsPrincipal? existsClaimsPrincipal = null)
    {
        return await InternalCreateAsync(Options, existsClaimsPrincipal, false);
    }

    /// <inheritdoc />
    /// <param name="existsClaimsPrincipal">已有主体, 可为 null</param>
    public virtual async Task<ClaimsPrincipal> CreateDynamicAsync(ClaimsPrincipal? existsClaimsPrincipal = null)
    {
        return await InternalCreateAsync(Options, existsClaimsPrincipal, true);
    }

    /// <summary>
    /// 按贡献者清单构建主体
    /// </summary>
    /// <param name="options">工厂选项</param>
    /// <param name="existsClaimsPrincipal">已存在的主体</param>
    /// <param name="isDynamic">是否跑动态贡献者</param>
    /// <returns>声明主体</returns>
    public virtual async Task<ClaimsPrincipal> InternalCreateAsync(
        SnowClaimsPrincipalFactoryOptions options,
        ClaimsPrincipal? existsClaimsPrincipal = null,
        bool isDynamic = false)
    {
        var claimsPrincipal = existsClaimsPrincipal ?? new ClaimsPrincipal(new ClaimsIdentity(
            AuthenticationType,
            SnowClaimTypes.UserName,
            SnowClaimTypes.Role));

        var context = new SnowClaimsPrincipalContributorContext(claimsPrincipal, ServiceProvider);

        if (!isDynamic)
        {
            foreach (var contributorType in options.Contributors)
            {
                var contributor = (ISnowClaimsPrincipalContributor)ServiceProvider.GetRequiredService(contributorType);
                await contributor.ContributeAsync(context);
            }
        }
        else
        {
            foreach (var contributorType in options.DynamicContributors)
            {
                var contributor = (ISnowDynamicClaimsPrincipalContributor)ServiceProvider.GetRequiredService(contributorType);
                await contributor.ContributeAsync(context);
            }
        }

        return context.ClaimsPrincipal;
    }
}
