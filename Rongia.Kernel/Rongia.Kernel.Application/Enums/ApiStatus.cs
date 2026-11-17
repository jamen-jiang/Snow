using System.ComponentModel;
using System.Reflection;

namespace Rongia.Kernel.Application.Enums;

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

/// <summary>
/// Api 状态扩展:读取 Description 标签、按状态码反查
/// (对应 Java ApiStatusEnum 的 getLabel/fromCode;Kernel.Application 不依赖 Rongia.Shared,自带一份)
/// </summary>
public static class ApiStatusExtensions
{
    /// <summary>
    /// 获取状态的标签:取 Description 特性值,没有时退回成员名
    /// </summary>
    public static string GetDescription(this ApiStatus status)
    {
        var field = typeof(ApiStatus).GetField(status.ToString(), BindingFlags.Public | BindingFlags.Static);
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? status.ToString();
    }

    /// <summary>
    /// 按状态码查找状态
    /// </summary>
    /// <returns>对应状态;找不到返回 null</returns>
    public static ApiStatus? FromCode(int code)
    {
        return Enum.IsDefined(typeof(ApiStatus), code) ? (ApiStatus)code : null;
    }
}
