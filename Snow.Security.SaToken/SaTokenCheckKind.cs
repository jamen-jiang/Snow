namespace Snow.Security.SaToken;

/// <summary>
/// 注解鉴权类型
/// </summary>
public enum SaTokenCheckKind
{
    /// <summary>
    /// 仅校验登录
    /// </summary>
    Login = 0,

    /// <summary>
    /// 校验权限
    /// </summary>
    Permission = 1,

    /// <summary>
    /// 校验角色
    /// </summary>
    Role = 2,
}
