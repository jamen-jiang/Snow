using System.Security.Claims;

namespace Snow.Security.Claims;

/// <summary>
/// 声明主体工厂(对应 ABP 的 <c>IAbpClaimsPrincipalFactory</c>)
/// </summary>
public interface ISnowClaimsPrincipalFactory
{
    /// <summary>
    /// 创建声明主体(跑静态贡献者)
    /// </summary>
    /// <param name="existsClaimsPrincipal">已存在的主体, 为空则新建</param>
    /// <returns>声明主体</returns>
    Task<ClaimsPrincipal> CreateAsync(ClaimsPrincipal? existsClaimsPrincipal = null);

    /// <summary>
    /// 创建声明主体(跑动态贡献者)
    /// </summary>
    /// <param name="existsClaimsPrincipal">已存在的主体, 为空则新建</param>
    /// <returns>声明主体</returns>
    Task<ClaimsPrincipal> CreateDynamicAsync(ClaimsPrincipal? existsClaimsPrincipal = null);
}
