using Snow.Logging;

namespace Snow.Exceptions;

/// <summary>
/// 业务异常标记接口(对应 ABP 的 <c>IBusinessException</c>)
/// <para>本接口没有任何成员, 只用于"这是一次业务规则不允许的失败"这一判断: 异常处理层据此转 400 并回错误码,
/// 不关心具体异常类型。继承它的异常同时提供错误码(<see cref="IHasErrorCode"/>)、
/// 附加数据(<see cref="IHasErrorDetails"/>)与日志级别(<see cref="IHasLogLevel"/>)。</para>
/// <para>存在的意义: 第三方可以自定义业务异常, 只实现本接口而不必继承 <see cref="BusinessException"/>,
/// 也能被框架识别为业务异常。</para>
/// </summary>
public interface IBusinessException : IHasErrorCode, IHasErrorDetails, IHasLogLevel
{
}
