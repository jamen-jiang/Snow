using Microsoft.AspNetCore.Authorization;

namespace Rongia.Security.SaToken;

/// <summary>
/// 校验登录: 标在控制器或动作上, 未登录返回 401
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class SaCheckLoginAttribute : AuthorizeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    public SaCheckLoginAttribute() => Policy = SaTokenPolicyProvider.LoginPolicy;
}
