using System;
using Microsoft.AspNetCore.Http;

namespace Snow.Http;

/// <summary>
/// <see cref="HttpRequest"/> 扩展(对应 ABP 的 <c>HttpRequestExtensions</c>).
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// 判断请求的 Accept 头是否接受指定类型(或通配 */*)
    /// </summary>
    /// <param name="request">HTTP 请求</param>
    /// <param name="contentType">MIME 类型</param>
    public static bool CanAccept(this HttpRequest request, string contentType)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(contentType);

        var accept = request.Headers.Accept;
        if (accept.Count == 0)
        {
            return false;
        }

        foreach (var item in accept)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            foreach (var part in item.Split(','))
            {
                var mediaType = part.Split(';')[0].Trim();
                if (string.Equals(mediaType, contentType, StringComparison.OrdinalIgnoreCase)
                    || mediaType == "*/*")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 判断是否为 AJAX 请求(依据 X-Requested-With 头)
    /// </summary>
    /// <param name="request">HTTP 请求</param>
    public static bool IsAjax(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return string.Equals(
            request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}
