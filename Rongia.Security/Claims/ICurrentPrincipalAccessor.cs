using System.Security.Claims;

namespace Rongia.Security.Claims;

/// <summary>
/// 当前主体访问器: 全框架读取"当前请求是谁"的唯一入口
/// <para>对齐 ABP 的 <c>ICurrentPrincipalAccessor</c>。业务代码不直接碰 HttpContext, 由本接口屏蔽宿主形态。</para>
/// </summary>
public interface ICurrentPrincipalAccessor
{
    /// <summary>
    /// 当前主体
    /// </summary>
    ClaimsPrincipal Principal { get; }

    /// <summary>
    /// 临时替换当前主体, 释放返回值后恢复
    /// </summary>
    /// <param name="principal">要生效的主体</param>
    /// <returns>释放后恢复原主体的句柄</returns>
    IDisposable Change(ClaimsPrincipal principal);
}
