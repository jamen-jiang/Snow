using Microsoft.AspNetCore.Authorization;

namespace Snow.Authentication;

/// <summary>
/// 校验登录: 标在控制器或动作上, 未登录返回 401
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class TokenCheckLoginAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    public TokenCheckLoginAttribute() => Policy = TokenPolicyProvider.LoginPolicy;
}
