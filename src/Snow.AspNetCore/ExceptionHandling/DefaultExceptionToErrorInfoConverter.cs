using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Exceptions;
using Snow.Validation;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 默认转换器: 校验异常/业务异常按原消息返回, 其余异常按选项决定是否暴露细节;
/// 错误码统一从 <see cref="IHasErrorCode"/> 取, 附加数据从 <see cref="IHasErrorDetails"/> 取。
/// </summary>
[SingletonService]
public sealed class DefaultExceptionToErrorInfoConverter : IExceptionToErrorInfoConverter
{
    private readonly IOptions<SnowExceptionHandlingOptions> _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">异常处理选项</param>
    public DefaultExceptionToErrorInfoConverter(IOptions<SnowExceptionHandlingOptions> options) => _options = options;

    /// <inheritdoc />
    public SnowRemoteServiceErrorInfo Convert(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        SnowExceptionHandlingOptions options = _options.Value;

        return exception switch
        {
            SnowValidationException validation => new SnowRemoteServiceErrorInfo
            {
                Code = "ValidationFailed",
                Message = validation.Message,
                ValidationErrors = ToValidationErrors(validation),
            },
            SnowUserFriendlyException friendly => new SnowRemoteServiceErrorInfo
            {
                Code = friendly.Code ?? "BusinessError",
                Message = friendly.Message,
                Details = options.SendExceptionDetailsToClients ? SafeToString(friendly) : null,
                Data = ToData(friendly.Details),
            },
            SnowBusinessException business => new SnowRemoteServiceErrorInfo
            {
                Code = business.Code ?? "BusinessError",
                Message = business.Message,
                Details = options.SendExceptionDetailsToClients ? SafeToString(business) : null,
                Data = options.SendExceptionDetailsToClients ? ToData(business.Details) : null,
            },
            UnauthorizedAccessException => new SnowRemoteServiceErrorInfo
            {
                Code = "Forbidden",
                Message = "没有访问该资源的权限",
            },
            KeyNotFoundException => new SnowRemoteServiceErrorInfo
            {
                Code = "NotFound",
                Message = exception.Message,
            },
            NotImplementedException => new SnowRemoteServiceErrorInfo
            {
                Code = "NotImplemented",
                Message = "该功能尚未实现",
            },
            ArgumentException or InvalidOperationException => new SnowRemoteServiceErrorInfo
            {
                Code = "InvalidArgument",
                Message = exception.Message,
                Details = options.SendExceptionDetailsToClients ? SafeToString(exception) : null,
            },
            // 其余 Snow 框架异常: 有错误码就用错误码; Warning 及以下说明是预期内的失败, 消息可回
            SnowException snow => new SnowRemoteServiceErrorInfo
            {
                Code = snow.Code ?? "InternalError",
                Message = options.SendExceptionDetailsToClients || snow.LogLevel <= LogLevel.Warning
                    ? snow.Message
                    : "服务器内部错误",
                Details = options.SendExceptionDetailsToClients ? SafeToString(snow) : null,
            },
            _ => new SnowRemoteServiceErrorInfo
            {
                Code = (exception as IHasErrorCode)?.Code ?? "InternalError",
                Message = options.SendExceptionDetailsToClients ? exception.Message : "服务器内部错误",
                Details = options.SendExceptionDetailsToClients ? SafeToString(exception) : null,
            },
        };
    }

    private static Dictionary<string, string[]> ToValidationErrors(SnowValidationException exception)
    {
        Dictionary<string, string[]> errors = new(StringComparer.Ordinal);

        foreach (System.ComponentModel.DataAnnotations.ValidationResult result in exception.ValidationErrors)
        {
            string[] members = result.MemberNames.Any() ? result.MemberNames.ToArray() : new[] { string.Empty };
            foreach (string member in members)
            {
                if (errors.TryGetValue(member, out string[]? existing))
                {
                    errors[member] = existing.Append(result.ErrorMessage ?? "参数不合法").ToArray();
                }
                else
                {
                    errors[member] = new[] { result.ErrorMessage ?? "参数不合法" };
                }
            }
        }

        return errors;
    }

    private static Dictionary<string, object?>? ToData(object? details)
        => details is null ? null : new Dictionary<string, object?> { ["details"] = details };

    private string SafeToString(Exception exception)
        => _options.Value.SendStackTraceToClients ? exception.ToString() : exception.GetType().FullName + ": " + exception.Message;
}
