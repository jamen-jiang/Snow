using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rongia.Authorization;
using Rongia.Modularity;
using Rongia.Security;

namespace Rongia.Security.SaToken;

/// <summary>
/// 登录态模块: 装配 Sa-Token 风格的令牌认证方案、登录态存储、注解鉴权与静态门面
/// <para>配置来自 <c>SaToken</c> 节; 宿主注册了 <see cref="IDistributedCache"/> 则自动改用分布式存储, 否则进程内存储。</para>
/// </summary>
[DependsOn(typeof(SecurityModule), typeof(AuthorizationModule))]
public sealed class SaTokenModule : Module
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IConfigurationSection section = context.Configuration.GetSection(SaTokenOptions.SectionName);
        SaTokenOptions options = section.Get<SaTokenOptions>() ?? new SaTokenOptions();
        options.Validate();

        context.Services.Configure<SaTokenOptions>(section);
        context.Services.AddHttpContextAccessor();

        // 默认进程内存储; 宿主提供了分布式缓存时替换为分布式实现(多实例共享)
        context.Services.AddSingleton<ISaTokenStore, MemorySaTokenStore>();
        if (context.Services.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
        {
            context.Services.Replace(ServiceDescriptor.Singleton<ISaTokenStore, DistributedCacheSaTokenStore>());
        }

        context.Services.AddSingleton<ISaTokenManager, SaTokenManager>();

        context.Services
            .AddAuthentication(SaTokenAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, SaTokenAuthenticationHandler>(SaTokenAuthenticationHandler.SchemeName, configureOptions: null);

        // 全站默认要求登录(对齐 Java 版 SaTokenConfig 的 /** 兜底 StpUtil.checkLogin()), 匿名接口用 [AllowAnonymous] 放行
        context.Services.AddAuthorization(authorizationOptions =>
        {
            authorizationOptions.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // 注解鉴权: 动态策略提供器 + 要求处理器
        context.Services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, SaTokenPolicyProvider>());
        context.Services.AddScoped<IAuthorizationHandler, SaTokenRequirementHandler>();
    }

    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        StpUtil.Initialize(context.ServiceProvider);
    }
}
