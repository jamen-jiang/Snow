using Microsoft.Extensions.Logging;
using Snow.Logging;

namespace Snow.Exceptions;

/// <summary>
/// 业务规则不允许时抛出的异常(对应 ABP 的 <c>BusinessException</c>)
/// <para>与 ABP 一致: 直接继承 <see cref="Exception"/>, 与框架异常根 <see cref="SnowException"/>(对应 <c>AbpException</c>)
/// 是两棵平行的树 —— 判断"这次失败是业务规则不允许"看 <see cref="IBusinessException"/> 接口, 不看是否继承框架基类。</para>
/// <para>三个属性与 ABP 同名同类型: <see cref="Code"/>(<see cref="string"/>)、<see cref="Details"/>(<see cref="string"/>)、<see cref="LogLevel"/>;
/// 结构化上下文用 <see cref="WithData"/> 写进 <see cref="System.Exception.Data"/>。</para>
/// <para>与 <see cref="UserFriendlyException"/> 的关系: 后者是它的子类, 消息一定返回给客户端。
/// 日志级别默认 Warning: 业务失败是预期内的, 不该按 Error 刷日志。</para>
/// </summary>
public class BusinessException : Exception,
    IBusinessException,
    IHasErrorCode,
    IHasErrorDetails,
    IHasLogLevel
{
    /// <summary>
    /// 错误码, 客户端据此分支处理
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 错误说明文本, 会随错误响应返回给客户端
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 异常处理层写日志用的级别
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// 构造器(与 ABP 同形: 全部可选, 不给就是空业务异常)
    /// </summary>
    /// <param name="code">业务错误码</param>
    /// <param name="message">异常消息</param>
    /// <param name="details">错误说明文本</param>
    /// <param name="innerException">内部异常</param>
    /// <param name="logLevel">日志级别, 默认 Warning</param>
    public BusinessException(
        string? code = null,
        string? message = null,
        string? details = null,
        Exception? innerException = null,
        LogLevel logLevel = LogLevel.Warning)
        : base(message, innerException)
    {
        Code = code;
        Details = details;
        LogLevel = logLevel;
    }

    /// <summary>
    /// 往异常的 <see cref="System.Exception.Data"/> 里塞一条键值上下文, 返回自身便于链式调用
    /// </summary>
    /// <param name="name">键</param>
    /// <param name="value">值</param>
    /// <returns>同一个异常实例</returns>
    public BusinessException WithData(string name, object value)
    {
        Data[name] = value;
        return this;
    }
}
