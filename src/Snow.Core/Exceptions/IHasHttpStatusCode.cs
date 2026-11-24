namespace Snow.Exceptions;

/// <summary>
/// 自带 HTTP 状态码的异常契约
/// <para>与 <see cref="IHasErrorCode"/> / <see cref="Snow.Logging.IHasLogLevel"/> / <see cref="IHasErrorDetails"/> 同一套思路:
/// 异常自己声明该以什么状态码回给调用方, 异常处理层只按接口取值, 不按具体类型硬编码,
/// 于是领域层可以表达 404 这类语义而不必让 <c>Snow.AspNetCore</c> 反向依赖 <c>Snow.Domain</c>。</para>
/// <para>与 ABP 最新的 <c>Volo.Abp.ExceptionHandling.IHasHttpStatusCode</c> 同名同形(属性都叫 <c>HttpStatusCode</c>):
/// 值是 400 / 404 / 409 这样的数字, 不是 <see cref="System.Net.HttpStatusCode"/> 枚举 —— 契约放在 <c>Snow.Core</c>,
/// 不该为此引入 ASP.NET 的常量类型。</para>
/// </summary>
public interface IHasHttpStatusCode
{
    /// <summary>
    /// 该异常对应的 HTTP 状态码
    /// </summary>
    int HttpStatusCode { get; }
}
