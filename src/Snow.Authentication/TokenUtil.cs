using Snow.Security.Claims;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.Security;

namespace Snow.Authentication;

/// <summary>
/// 静态门面: 只保留"拿当前请求"这一件注入做不了的事(取登录Id、判登录、判角色、取原始令牌)
/// <para>由 <see cref="SnowAuthenticationModule"/> 在应用初始化阶段注入服务定位器, 之后即可全局调用。</para>
/// <para>登录、登出、踢人、封禁、权限校验都不在这里: 那些用构造注入 <see cref="ITokenService"/> 与
/// <c>IPermissionChecker</c>, 静态转发只会多一层、还会让单元测试没法替换。</para>
/// </summary>
public static class TokenUtil
{
    private static IServiceProvider? _rootProvider;
    private static IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// 由模块在应用初始化阶段调用, 注入服务定位入口
    /// </summary>
    /// <param name="serviceProvider">根服务提供者</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _rootProvider = serviceProvider;
        _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
    }

    /// <summary>
    /// 令牌服务
    /// </summary>
    public static ITokenService TokenService => Provider.GetRequiredService<ITokenService>();

    /// <summary>
    /// JWT 配置
    /// </summary>
    public static SnowAuthenticationOptions Options => Provider.GetRequiredService<IOptionsMonitor<SnowAuthenticationOptions>>().CurrentValue;

    /// <summary>
    /// 当前请求
    /// </summary>
    // 这里必须直接用 _rootProvider: Provider 会读 Context, 两边互相引用就是无限递归
    public static HttpContext? Context => _httpContextAccessor?.HttpContext ?? _rootProvider?.GetService<IHttpContextAccessor>()?.HttpContext;

    /// <summary>
    /// 是否已登录当前请求(读当前主体, 不查存储)
    /// <para>依赖认证中间件已经跑过: 直接读 <c>HttpContext.User</c>, 不会主动触发校验。</para>
    /// </summary>
    public static bool IsLogin() => Context?.User.Identity?.IsAuthenticated == true;

    /// <summary>
    /// 取当前登录Id, 未登录返回 null(读当前主体, 不查存储)
    /// </summary>
    public static long? GetLoginIdOrNull()
    {
        string? value = Context?.User.FindFirst(SnowClaimTypes.UserId)?.Value;
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id) ? id : null;
    }

    /// <summary>
    /// 取当前登录Id, 未登录抛 <see cref="TokenNotLoginException"/>
    /// </summary>
    public static long GetLoginId() => GetLoginIdOrNull() ?? throw new TokenNotLoginException();

    /// <summary>
    /// 校验登录, 未登录抛 <see cref="TokenNotLoginException"/>
    /// </summary>
    public static void CheckLogin()
    {
        if (!IsLogin())
        {
            throw new TokenNotLoginException();
        }
    }

    /// <summary>
    /// 是否属于任一角色(读当前主体, 不查存储)
    /// </summary>
    /// <param name="roles">角色名</param>
    public static bool HasRole(params string[] roles)
    {
        ClaimsPrincipal? principal = Context?.User;
        return principal is not null && roles.Any(principal.IsInRole);
    }

    /// <summary>
    /// 校验角色, 缺失抛 <see cref="TokenNotRoleException"/>
    /// </summary>
    /// <param name="roles">角色名</param>
    public static void CheckRole(params string[] roles)
    {
        CheckLogin();
        if (!HasRole(roles))
        {
            throw new TokenNotRoleException(roles);
        }
    }

    /// <summary>
    /// 取当前请求携带的原始令牌(不含前缀), 没带返回 null
    /// </summary>
    public static string? GetTokenValue()
    {
        return TokenResolver.ResolveToken(Context);
    }

    private static IServiceProvider Provider => Context?.RequestServices ?? _rootProvider
        ?? throw new InvalidOperationException("令牌门面尚未初始化: 请确认宿主模块的 [DependsOn] 中包含 SnowAuthenticationModule。");
}
