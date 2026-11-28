namespace Snow.Http;

/// <summary>
/// 校验错误信息(对应 ABP 的 <c>RemoteServiceValidationErrorInfo</c>)
/// </summary>
[Serializable]
public class RemoteServiceValidationErrorInfo
{
    /// <summary>
    /// 校验错误消息
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// 相关的无效成员(字段/属性)
    /// </summary>
    public string[] Members { get; set; } = null!;

    /// <summary>
    /// 构造器
    /// </summary>
    public RemoteServiceValidationErrorInfo()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">校验错误消息</param>
    public RemoteServiceValidationErrorInfo(string message)
    {
        Message = message;
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">校验错误消息</param>
    /// <param name="members">相关的无效成员</param>
    public RemoteServiceValidationErrorInfo(string message, string[] members)
        : this(message)
    {
        Members = members;
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">校验错误消息</param>
    /// <param name="member">相关的无效成员</param>
    public RemoteServiceValidationErrorInfo(string message, string member)
        : this(message, new[] { member })
    {
    }
}
