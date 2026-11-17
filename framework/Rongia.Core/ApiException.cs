namespace Rongia.Core;

/// <summary>
/// 自定义业务异常
/// </summary>
public class ApiException : System.Exception
{
    private int _code;
    private readonly string? _message;

    /// <summary>
    /// 错误代码;未设置时取 FAIL
    /// </summary>
    public int Code
    {
        get => _code == 0 ? (int)ApiStatus.Fail : _code;
        set => _code = value;
    }

    /// <summary>
    /// 默认构造器;code 取 FAIL
    /// </summary>
    public ApiException()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    public ApiException(int code)
    {
        _code = code;
    }

    /// <summary>
    /// 构造器
    /// </summary>
    public ApiException(ApiStatus code)
    {
        _code = (int)code;
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">错误信息,可空</param>
    public ApiException(string? message) : base(message)
    {
        _message = message;
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="code">错误代码</param>
    /// <param name="message">错误信息,可空</param>
    public ApiException(int code, string? message) : base(message)
    {
        _code = code;
        _message = message;
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="code">状态枚举</param>
    /// <param name="message">错误信息,可空</param>
    public ApiException(ApiStatus code, string? message) : base(message)
    {
        _code = (int)code;
        _message = message;
    }

    /// <summary>
    /// 获取错误信息;自定义为空时回退到基类的 Message
    /// </summary>
    public override string Message => string.IsNullOrEmpty(_message) ? base.Message : _message;
}
