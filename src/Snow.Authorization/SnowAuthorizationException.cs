using System;
using Snow.Exceptions;

namespace Snow.Authorization;

/// <summary>
/// 授权失败异常(对应 ABP 的 <c>AbpAuthorizationException</c>): 当前主体不满足权限要求时抛出,
/// 由异常过滤器特殊处理交给授权异常处理器(质询/禁止)。
/// </summary>
[Serializable]
public class SnowAuthorizationException : SnowException
{
    /// <summary>
    /// 构造器
    /// </summary>
    public SnowAuthorizationException()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public SnowAuthorizationException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public SnowAuthorizationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
