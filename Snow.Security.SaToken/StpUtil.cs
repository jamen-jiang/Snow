using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Snow.Authorization.Permissions;
using Snow.Security;

namespace Snow.Security.SaToken;

/// <summary>
/// 静态门面(对齐 Sa-Token 的 StpUtil): 业务代码一行完成登录、登出、取登录Id、鉴权
/// <para>由 <see cref="SnowSaTokenModule"/> 在应用初始化阶段注入服务定位器, 之后即可全局调用。</para>
/// </summary>
public static class StpUtil
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
    /// 登录态管理器
    /// </summary>
    public static ISaTokenManager Manager => Provider.GetRequiredService<ISaTokenManager>();

    /// <summary>
    /// 登录态配置
    /// </summary>
    public static SaTokenOptions Options => Provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SaTokenOptions>>().Value;

    /// <summary>
    /// 当前请求
    /// </summary>
    public static HttpContext? Context => _httpContextAccessor?.HttpContext;

    /// <summary>
    /// 登录并返回令牌(只给用户Id)
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="claims">附加声明</param>
    /// <returns>令牌</returns>
    public static string Login(long userId, params Claim[] claims)
        => LoginAsync(userId, claims).GetAwaiter().GetResult();

    /// <summary>
    /// 登录并返回令牌(带账号与名称)
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="userName">登录账号</param>
    /// <param name="name">用户名称</param>
    /// <param name="claims">附加声明</param>
    /// <returns>令牌</returns>
    public static string Login(long userId, string userName, string name, params Claim[] claims)
        => Manager.LoginAsync(userId, userName, name, ToPairs(claims)).GetAwaiter().GetResult();

    /// <summary>
    /// 登出当前请求的令牌
    /// </summary>
    public static void Logout() => Logout(GetTokenValue() ?? throw new SaNotLoginException("当前请求未携带令牌。"));

    /// <summary>
    /// 登出指定令牌
    /// </summary>
    /// <param name="token">令牌</param>
    public static void Logout(string token) => Manager.LogoutAsync(token).GetAwaiter().GetResult();

    /// <summary>
    /// 是否已登录当前请求
    /// </summary>
    public static bool IsLogin() => GetLoginIdOrNull() is not null;

    /// <summary>
    /// 取当前登录Id, 未登录返回 null
    /// </summary>
    public static long? GetLoginIdOrNull()
    {
        string? value = Context?.User.FindFirst(UserClaimTypes.UserId)?.Value;
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id) ? id : null;
    }

    /// <summary>
    /// 取当前登录Id, 未登录抛 <see cref="SaNotLoginException"/>
    /// </summary>
    public static long GetLoginId() => GetLoginIdOrNull() ?? throw new SaNotLoginException();

    /// <summary>
    /// 校验登录, 未登录抛 <see cref="SaNotLoginException"/>
    /// </summary>
    public static void CheckLogin()
    {
        if (!IsLogin())
        {
            throw new SaNotLoginException();
        }
    }

    /// <summary>
    /// 是否具备全部权限
    /// </summary>
    /// <param name="permissions">权限标识</param>
    public static bool HasPermission(params string[] permissions)
    {
        ClaimsPrincipal? principal = Context?.User;
        IPermissionChecker checker = Provider.GetRequiredService<IPermissionChecker>();
        return permissions.All(permission => checker.IsGrantedAsync(principal, permission).GetAwaiter().GetResult());
    }

    /// <summary>
    /// 校验权限, 缺失抛 <see cref="SaNotPermissionException"/>
    /// </summary>
    /// <param name="permissions">权限标识</param>
    public static void CheckPermission(params string[] permissions)
    {
        CheckLogin();
        if (!HasPermission(permissions))
        {
            throw new SaNotPermissionException(permissions);
        }
    }

    /// <summary>
    /// 是否属于任一角色
    /// </summary>
    /// <param name="roles">角色名</param>
    public static bool HasRole(params string[] roles)
    {
        ClaimsPrincipal? principal = Context?.User;
        return principal is not null && roles.Any(principal.IsInRole);
    }

    /// <summary>
    /// 校验角色, 缺失抛 <see cref="SaNotRoleException"/>
    /// </summary>
    /// <param name="roles">角色名</param>
    public static void CheckRole(params string[] roles)
    {
        CheckLogin();
        if (!HasRole(roles))
        {
            throw new SaNotRoleException(roles);
        }
    }

    /// <summary>
    /// 踢人: 删除该账号全部登录态
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <returns>被踢下线的数量</returns>
    public static int Kickout(long userId) => Manager.KickoutAsync(userId).GetAwaiter().GetResult();

    /// <summary>
    /// 封禁账号
    /// </summary>
    /// <param name="userId">用户Id</param>
    /// <param name="duration">封禁时长, null 表示永久</param>
    /// <returns>被踢下线的数量</returns>
    public static int Disable(long userId, TimeSpan? duration = null) => Manager.DisableAsync(userId, duration).GetAwaiter().GetResult();

    /// <summary>
    /// 解除封禁
    /// </summary>
    /// <param name="userId">用户Id</param>
    public static void Enable(long userId) => Manager.EnableAsync(userId).GetAwaiter().GetResult();

    /// <summary>
    /// 是否处于封禁中
    /// </summary>
    /// <param name="userId">用户Id</param>
    public static bool IsDisable(long userId) => Manager.IsDisabledAsync(userId).GetAwaiter().GetResult();

    /// <summary>
    /// 当前请求携带的令牌
    /// </summary>
    public static string? GetTokenValue() => SaTokenResolver.ResolveToken(Context, Options);

    /// <summary>
    /// 当前令牌信息, 无令牌返回 null
    /// </summary>
    public static SaTokenInfo? GetTokenInfo()
    {
        string? token = GetTokenValue();
        return string.IsNullOrEmpty(token) ? null : Manager.GetTokenInfoAsync(token).GetAwaiter().GetResult();
    }

    private static async Task<string> LoginAsync(long userId, Claim[] claims)
        => await Manager.LoginAsync(userId, ToPairs(claims));

    private static IEnumerable<KeyValuePair<string, string>>? ToPairs(Claim[] claims)
        => claims.Length == 0 ? null : claims.Select(claim => new KeyValuePair<string, string>(claim.Type, claim.Value));

    private static IServiceProvider Provider => Context?.RequestServices ?? _rootProvider
        ?? throw new InvalidOperationException("SaToken 尚未初始化: 请确认宿主模块的 [DependsOn] 中包含 SnowSaTokenModule。");
}
