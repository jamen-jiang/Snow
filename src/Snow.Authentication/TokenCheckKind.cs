namespace Snow.Authentication;

/// <summary>
/// 注解鉴权类型
/// </summary>
public enum TokenCheckKind
{
    /// <summary>
    /// 仅校验登录
    /// </summary>
    Login = 0,

    /// <summary>
    /// 校验角色
    /// </summary>
    Role = 1,
}
