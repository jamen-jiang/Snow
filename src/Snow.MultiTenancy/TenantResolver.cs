using Snow.DependencyInjection;

namespace Snow.MultiTenancy;

/// <summary>
/// 租户解析器的默认实现: 依次执行全部出资者, 第一个给出结果的胜出
/// </summary>
[TransientService]
public sealed class TenantResolver : ITenantResolver
{
    private readonly IEnumerable<ITenantResolveContributor> _contributors;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="contributors">全部出资者, 按注册顺序执行</param>
    /// <param name="serviceProvider">传给解析上下文的服务提供者</param>
    public TenantResolver(IEnumerable<ITenantResolveContributor> contributors, IServiceProvider serviceProvider)
    {
        _contributors = contributors;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<TenantResolveResult> ResolveTenantIdOrNameAsync()
    {
        TenantResolveResult result = new();
        TenantResolveContext context = new(_serviceProvider);

        foreach (ITenantResolveContributor contributor in _contributors)
        {
            await contributor.ResolveAsync(context).ConfigureAwait(false);

            if (!context.Handled)
            {
                continue;
            }

            if (Guid.TryParse(context.TenantIdOrName, out Guid tenantId))
            {
                result.TenantId = tenantId;
            }
            else
            {
                result.TenantName = context.TenantIdOrName;
            }

            break;
        }

        return result;
    }
}
