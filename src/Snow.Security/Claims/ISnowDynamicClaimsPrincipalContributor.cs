namespace Snow.Security.Claims;

/// <summary>
/// 动态声明贡献者: 只在需要动态刷新声明(如权限变更)时参与构建
/// (对应 ABP 的 <c>IAbpDynamicClaimsPrincipalContributor</c>)
/// </summary>
public interface ISnowDynamicClaimsPrincipalContributor
{
    /// <summary>
    /// 贡献声明
    /// </summary>
    /// <param name="context">贡献上下文</param>
    /// <returns>表示贡献过程的任务</returns>
    Task ContributeAsync(SnowClaimsPrincipalContributorContext context);
}
