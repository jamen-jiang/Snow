using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Snow.Content;

namespace Snow.AspNetCore.Mvc.ContentFormatters;

/// <summary>
/// 远程流内容输出格式化器, 对应 ABP 的 <c>RemoteStreamContentOutputFormatter</c>。
/// </summary>
public class RemoteStreamContentOutputFormatter : OutputFormatter
{
    /// <summary>
    /// 创建远程流内容输出格式化器。
    /// </summary>
    public RemoteStreamContentOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("*/*"));
    }

    /// <summary>
    /// 判断是否可写入指定类型。
    /// </summary>
    /// <param name="type">待写入类型</param>
    /// <returns>是否可写入</returns>
    protected override bool CanWriteType(Type? type)
    {
        return typeof(IRemoteStreamContent).IsAssignableFrom(type);
    }

    /// <summary>
    /// 写入远程流内容响应体。
    /// </summary>
    /// <param name="context">输出格式化上下文</param>
    public async override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var remoteStream = (IRemoteStreamContent?)context.Object;

        if (remoteStream != null)
        {
            context.HttpContext.Response.ContentType = remoteStream.ContentType;
            context.HttpContext.Response.ContentLength = remoteStream.ContentLength;

            if (!remoteStream.FileName.IsNullOrWhiteSpace() &&
                !context.HttpContext.Response.Headers.ContainsKey(HeaderNames.ContentDisposition))
            {
                var contentDisposition = new ContentDispositionHeaderValue("attachment");
                contentDisposition.SetHttpFileName(remoteStream.FileName);
                context.HttpContext.Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
            }

            var cancellationToken = context.HttpContext.RequestAborted;

            using (remoteStream)
            {
                var stream = remoteStream.GetStream();

                try
                {
                    await stream.CopyToAsync(context.HttpContext.Response.Body, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // The request was aborted, nothing can be written to the response anymore.
                }
            }
        }
    }
}
