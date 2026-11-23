using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Snow.AspNetCore;
using Snow.Authorization;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Security;
using Snow.Timing;

namespace Snow.Authentication;

/// <summary>
/// 认证模块: 验签交给框架的 <c>JwtBearer</c>, 本包只补它没有的两件事 —— 签发令牌与会话白名单
/// <para><b>验签</b>: 注册一个 <see cref="JwtBearerHandler"/>(方案名 <see cref="SchemeName"/>)。验 <c>iss</c>/<c>aud</c>/签名、
/// 拼 <c>WWW-Authenticate</c> 全由框架完成, 本包不自己写 <c>AuthenticationHandler</c>。</para>
/// <para><b>签发</b>: <see cref="TokenService"/> 用 <see cref="JsonWebTokenHandler"/> 以 HS256 签发,
/// 对称密钥同时塞进 <see cref="TokenValidationParameters.IssuerSigningKeys"/> 供框架验签。</para>
/// <para><b>会话</b>: 微软的处理器是无状态的, 认不出"这个令牌已经被踢了"。所以每次验签通过后再查一次
/// <see cref="ITokenSessionStore"/>: 登出、踢人、封禁删掉会话, 令牌立刻失效。</para>
/// <para>状态存储只有 <see cref="DistributedCacheTokenSessionStore"/> 一个实现, 跑在 <see cref="IDistributedCache"/> 上。
/// 本模块<b>不注册</b> <see cref="IDistributedCache"/>, 只在解析会话存储时取容器里已有的实例: 宿主注册了 Redis 就用 Redis,
/// 什么都没注册才回退进程内实现(<see cref="MemoryDistributedCache"/>)。因此与缓存模块的装配顺序无关。</para>
/// </summary>
[DependsOn(typeof(SnowSecurityModule), typeof(SnowAuthorizationModule), typeof(SnowTimingModule), typeof(SnowAspNetCoreModule))]
public sealed class SnowAuthenticationModule : SnowModule
{
    /// <summary>
    /// 本模块登记的认证方案名
    /// </summary>
    public const string SchemeName = "SnowAuthentication";

    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IConfiguration configuration = context.Services.GetConfiguration();
        IConfigurationSection section = configuration.GetSection("Authentication");

        // 启动时先绑一次并校验: 配置写错就直接起不来, 而不是等到第一个请求进来才发现全是 401
        SnowAuthenticationOptions options = section.Get<SnowAuthenticationOptions>() ?? new SnowAuthenticationOptions();
        options.Validate();

        // 校验同时挂在选项管线上: 热更新后也会校验, 不会出现"密钥被改成空串但进程照跑"
        context.Services.AddOptions<SnowAuthenticationOptions>()
            .Bind(section)
            .Validate(authenticationOptions =>
            {
                authenticationOptions.Validate();
                return true;
            });

        context.Services.AddHttpContextAccessor();

        // 兜底缓存只在宿主一个都没注册时才用得上, 而且以"备用品"的身份登记: 不占用 IDistributedCache 这个槽,
        // 所以宿主之后调用 AddStackExchangeRedisCache / AddDistributedMemoryCache 都能正常生效。
        context.Services.TryAddSingleton(serviceProvider =>
            new MemoryDistributedCache(serviceProvider.GetRequiredService<IOptions<MemoryDistributedCacheOptions>>()));

        // TryAdd: 宿主已经登记过自己的实现就不覆盖
        context.Services.TryAddSingleton<ITokenSessionStore>(serviceProvider => new DistributedCacheTokenSessionStore(
            serviceProvider.GetService<IDistributedCache>() ?? serviceProvider.GetRequiredService<MemoryDistributedCache>(),
            serviceProvider.GetRequiredService<IClock>()));
        // TokenService 与 OptionsTokenKeyProvider 的登记由类上的 [SingletonService] 完成。
        // 约定注册会把接口描述符指向具体类型描述符的实例, 解析 ITokenService 与 TokenService 得到同一个对象,
        // 所以这里不再需要手工 Replace 把两个描述符缝起来。

        // 方案本身用框架的 JwtBearer: 只登记, 不抢宿主的默认方案
        context.Services
            .AddAuthentication()
            .AddJwtBearer(SchemeName, configureOptions: _ => { });

        // 方案的选项从选项监视器读: 改配置不用重启。放在 AddJwtBearer 之后, 所以能盖掉它的默认值
        context.Services.AddOptions<JwtBearerOptions>(SchemeName)
            .Configure<IOptionsMonitor<SnowAuthenticationOptions>, ITokenKeyProvider>((jwtOptions, optionsMonitor, keyProvider) =>
                Configure(jwtOptions, optionsMonitor.CurrentValue, keyProvider));

        // 宿主已设过默认(AddAuthentication("Xxx"))就沿用, 没设才用本方案。
        // 只写 DefaultScheme: 另两项留空, 框架解析时自动回退到它, 这样宿主任何时候显式指定都能盖过本模块
        context.Services.PostConfigure<AuthenticationOptions>(authenticationOptions => authenticationOptions.DefaultScheme ??= SchemeName);
        context.Services.AddAuthorization();
        context.Services.PostConfigure<AuthorizationOptions>(authorizationOptions =>
            authorizationOptions.FallbackPolicy ??= new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // 注解鉴权: 动态策略提供器 + 要求处理器
        // TokenPolicyProvider 上标了 [SingletonService(ReplaceServices = true)], 由约定注册顶掉框架自带的
        // DefaultAuthorizationPolicyProvider; snowauth:* 策略只有它认得, 宿主若要自定义策略解析应继承或包装它。

        // 登录态异常转 401/403: MVC 管道里用过滤器。必须用 AddService 登记 —— 它生成 ServiceFilterAttribute,
        // SnowMvcActionDescriptorProvider 才认得出这是框架过滤器, [DisableSnowFeatures(DisableMvcFilters = true)] 才摘得掉。
        // 最小 API 没有 MVC 过滤器这一层, 走中间件: 宿主在管道里挂 UseSnowTokenExceptionHandling()。
        context.Services.AddTransient<TokenExceptionFilter>();
        context.Services.Configure<MvcOptions>(mvcOptions => mvcOptions.Filters.AddService(typeof(TokenExceptionFilter)));
    }

    /// <inheritdoc />
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 进程内缓存只对单实例有效: 多实例部署时 A 实例删掉的会话到 B 实例还能查到, 表现为"登出后换个实例又活了"。
        // 缺 Redis 属于配置问题, 这里只提醒, 不阻断启动
        IDistributedCache? cache = context.ServiceProvider.GetService<IDistributedCache>();
        if (cache is null or MemoryDistributedCache)
        {
            context.ServiceProvider.GetService<ILogger<SnowAuthenticationModule>>()?.LogWarning(
                "Snow.Authentication 正在使用进程内缓存保存会话: 登出、踢人、封禁只在当前实例生效。多实例部署请注册 IDistributedCache(如 Snow.Caching.StackExchangeRedis)。");
        }

        TokenUtil.Initialize(context.ServiceProvider);
    }

    /// <summary>
    /// 把配置翻成 <see cref="JwtBearerOptions"/>
    /// </summary>
    /// <param name="jwtOptions">框架的处理器选项</param>
    /// <param name="options">本包配置</param>
    /// <param name="keyProvider">密钥解析器</param>
    private static void Configure(JwtBearerOptions jwtOptions, SnowAuthenticationOptions options, ITokenKeyProvider keyProvider)
    {
        // 关掉声明改名: 框架默认会把 sub 改写成 nameidentifier 之类的长 URI, 读声明时就认不出原名了
        jwtOptions.MapInboundClaims = false;

        TokenKeySet keys = keyProvider.Resolve(options);

        TokenValidationParameters validation = jwtOptions.TokenValidationParameters;
        validation.ValidateIssuer = true;
        validation.ValidIssuer = options.GetIssuer();
        validation.ValidateAudience = true;
        validation.ValidAudience = options.GetAudience();
        // 关掉框架的有效期校验: 令牌里的 exp 只是签发时的快照, 真正判过期的是服务端会话(会滑动续期)。
        // 开着它会在会话还活着的时候先把令牌拒掉, 滑动续期就白做了
        validation.ValidateLifetime = false;
        validation.ValidateIssuerSigningKey = true;
        validation.IssuerSigningKeys = keys.ValidationKeys;
        // 白名单严格等于这一个算法: 少了这一步就有算法混淆攻击(把 alg 改成 none 或另一种算法再签一份)
        validation.ValidAlgorithms = keys.ValidAlgorithms;
        validation.NameClaimType = UserClaimTypes.UserName;
        validation.RoleClaimType = UserClaimTypes.Role;

        // 只挂自己的回调, 不整体替换 Events: 宿主在 AddJwtBearer 里配的 OnMessageReceived / OnChallenge /
        // OnAuthenticationFailed 要留着, 否则接入方一个都改不了
        jwtOptions.Events ??= new JwtBearerEvents();
        Func<TokenValidatedContext, Task>? existing = jwtOptions.Events.OnTokenValidated;
        jwtOptions.Events.OnTokenValidated = existing is null
            ? ValidateAsync
            : async tokenContext =>
            {
                await existing(tokenContext);
                if (tokenContext.Result is null || tokenContext.Result.Succeeded)
                {
                    await ValidateAsync(tokenContext);
                }
            };
    }

    /// <summary>
    /// 验签通过之后再查一次会话: 框架的处理器只验签名, 认不出"这个令牌已经被踢了"
    /// </summary>
    /// <param name="context">令牌校验上下文</param>
    /// <returns>表示处理过程的任务</returns>
    private static async Task ValidateAsync(TokenValidatedContext context)
    {
        ClaimsPrincipal? principal = context.Principal;
        if (principal is null)
        {
            context.Fail("令牌里没有身份。");
            return;
        }

        string? jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti))
        {
            context.Fail("令牌里没有 jti, 无法确认会话。");
            return;
        }

        TokenService? tokenService = context.HttpContext.RequestServices.GetService<TokenService>();
        if (tokenService is not null && await tokenService.RenewSessionAsync(jti, context.HttpContext.RequestAborted) is null)
        {
            context.Fail("令牌已失效。");
        }
    }
}