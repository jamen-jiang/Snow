using System.ComponentModel;

namespace Rongia.Core;

/// <summary>
/// Api 状态(对应 Java ApiStatusEnum;枚举底层值即状态码,标签在 Description)
/// </summary>
public enum ApiStatus
{
    /// <summary>
    /// 成功
    /// </summary>
    [Description("成功")]
    Success = 200,

    /// <summary>
    /// 失败(业务错误)
    /// </summary>
    [Description("失败")]
    Fail = 400,

    /// <summary>
    /// 未登录/令牌失效(过期、被顶下线等具体原因用 message 区分)
    /// </summary>
    [Description("未登录")]
    Unauthorized = 401,

    /// <summary>
    /// 没访问权限
    /// </summary>
    [Description("没访问权限")]
    Forbidden = 403,

    /// <summary>
    /// 系统异常
    /// </summary>
    [Description("系统异常")]
    Error = 500,
}
