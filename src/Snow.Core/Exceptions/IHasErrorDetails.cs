namespace Snow.Exceptions;

/// <summary>
/// 带附加数据的异常契约(对应 ABP 的 <c>IHasErrorDetails</c>)
/// <para>与 ABP 的差异: ABP 的 Details 是 string(错误说明文本), Snow 的 Details 是 object,
/// 会被原样序列化进错误响应的 data 字段, 便于回传业务上下文。</para>
/// </summary>
public interface IHasErrorDetails
{
    /// <summary>
    /// 附加数据, 会原样序列化给客户端
    /// </summary>
    object? Details { get; }
}
