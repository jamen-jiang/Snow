using System;
using Microsoft.AspNetCore.Builder;

namespace Snow.Authentication;

/// <summary>
/// 注册辅助
/// </summary>
public static class TokenExceptionHandlingExtensions
{
    /// <summary>
    /// 挂上登录态异常中间件
    /// <para>由 <c>SnowAuthenticationModule</c> 登记成管道贡献者, 挂在 <c>OnBeforeRouting</c> —— 位置在 <c>UseAuthentication()</c> 之前,
    /// 这样认证阶段抛的令牌异常也在它内层。MVC 宿主另有 <see cref="TokenExceptionFilter"/> 覆盖控制器内的同一批异常。</para>
    /// <para>挂在 <c>UseAuthentication()</c> 之后只覆盖控制器与端点里的异常, <c>JwtBearer</c> 在认证阶段抛的令牌异常会漏成 500。</para>
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>同一个应用构建器, 便于链式调用</returns>
    public static IApplicationBuilder UseSnowTokenExceptionHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<TokenExceptionMiddleware>();
    }
}
