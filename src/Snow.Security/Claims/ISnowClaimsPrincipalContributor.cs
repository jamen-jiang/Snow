namespace Snow.Security.Claims;

/// <summary>
/// 声明主体贡献者: 在构建 <see cref="System.Security.Claims.ClaimsPrincipal"/> 时往里追加声明
/// (对应 ABP 的 <c>IAbpClaimsPrincipalContributor</c>)
/// </summary>
public interface ISnowClaimsPrincipalContributor
{
    /// <summary>
    /// 贡献声明
    /// </summary>
    /// <param name="context">贡献上下文</param>
    /// <returns>表示贡献过程的任务</returns>
    Task ContributeAsync(SnowClaimsPrincipalContributorContext context);
}
