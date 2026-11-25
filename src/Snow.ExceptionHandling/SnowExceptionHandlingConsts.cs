namespace Snow.ExceptionHandling;

/// <summary>
/// 异常处理相关的约定字符串(对应 ABP 的 <c>AbpExceptionHandlingConsts</c>)。
/// <para>下游服务把这类"机器可读"标记放在错误消息或细节里, 上层据此翻译成面向终端用户的文案。</para>
/// </summary>
public static class SnowExceptionHandlingConsts
{
    /// <summary>
    /// 未授权标记: 出现在 <see cref="Snow.Http.RemoteServiceErrorInfo.Message"/> 里
    /// </summary>
    public const string Unauthorized = "Unauthorized";

    /// <summary>
    /// 无效令牌标记(对应 ABP 同名常量, 供认证层复用)
    /// </summary>
    public const string InvalidToken = "invalid_token";

    /// <summary>
    /// 会话过期标记: 出现在 <see cref="Snow.Http.RemoteServiceErrorInfo.Details"/> 里
    /// </summary>
    public const string SessionExpired = "SessionExpired";
}
