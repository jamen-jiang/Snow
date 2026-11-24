namespace Snow.Exceptions;

/// <summary>
/// 带附加说明的异常契约(对应 ABP 的 <c>IHasErrorDetails</c>): <see cref="Details"/> 是一段错误说明文本。
/// <para>与 ABP 一致, 类型是 <see cref="string"/>; 结构化的键值上下文请用 <see cref="BusinessException.WithData"/>(写进 <see cref="System.Exception.Data"/>)。</para>
/// </summary>
public interface IHasErrorDetails
{
    /// <summary>
    /// 错误说明文本, 会随错误响应返回给客户端
    /// </summary>
    string? Details { get; }
}
