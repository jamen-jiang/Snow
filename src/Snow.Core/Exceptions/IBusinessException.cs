using Snow.Logging;

namespace Snow.Exceptions;

/// <summary>
/// 业务异常标记接口(对应 ABP 的 <c>IBusinessException</c>)
/// <para>本接口没有任何成员, 只用于"这是一次业务规则不允许的失败"这一判断: 异常处理层据此转 400 并回错误码,
/// 不关心具体异常类型。与 ABP 一致它是纯标记: 不代表实现者一定带错误码/说明/日志级别。
/// 这些能力由具体异常另行实现 <see cref="IHasErrorCode"/>(<c>Code</c>)、<see cref="IHasErrorDetails"/>(<c>Details</c>)、
/// <see cref="Snow.Logging.IHasLogLevel"/>(<c>LogLevel</c>), 异常处理层按需用模式匹配取用, 没实现就取兜底值。</para>
/// <para>存在的意义: 第三方可以自定义业务异常, 只实现本接口而不必继承 <see cref="BusinessException"/>,
/// 也能被框架识别为业务异常。</para>
/// </summary>
public interface IBusinessException
{
}
