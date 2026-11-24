using System.Reflection;
using Snow.Aop;

namespace Snow.Auditing;

/// <summary>
/// 审计辅助器: 判断是否需要审计, 以及从调用上下文组装审计日志
/// </summary>
public interface IAuditingHelper
{
    /// <summary>
    /// 判断某个方法是否应当审计
    /// </summary>
    /// <param name="method">被调用的方法</param>
    /// <returns>需要审计时返回 true</returns>
    bool ShouldSaveAudit(MethodInfo method);

    /// <summary>
    /// 创建一条空的审计日志并填好调用方与 HTTP 上下文
    /// </summary>
    /// <returns>审计日志</returns>
    AuditLogInfo CreateAuditLogInfo();

    /// <summary>
    /// 按一次调用创建审计动作
    /// </summary>
    /// <param name="context">方法调用上下文</param>
    /// <returns>审计动作</returns>
    AuditLogActionInfo CreateAuditLogActionInfo(AspectContext context);

    /// <summary>
    /// 刷新 HTTP 相关字段(方法执行完后状态码才确定)
    /// </summary>
    /// <param name="logInfo">审计日志</param>
    void FillHttpContext(AuditLogInfo logInfo);
}
