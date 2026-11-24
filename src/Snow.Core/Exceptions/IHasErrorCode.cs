namespace Snow.Exceptions;

/// <summary>
/// 带错误码的异常契约(对应 ABP 的 <c>IHasErrorCode</c>)
/// <para>异常处理层只看这个接口取错误码, 不关心具体异常类型。</para>
/// </summary>
public interface IHasErrorCode
{
    /// <summary>
    /// 错误码, 客户端据此分支处理; 为 null 时用异常处理层的兜底码
    /// </summary>
    string? Code { get; }
}
