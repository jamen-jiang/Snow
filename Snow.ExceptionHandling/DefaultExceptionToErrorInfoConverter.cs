using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Validation;

namespace Snow.ExceptionHandling;

/// <summary>
/// 默认转换器: 业务异常/校验异常按原消息返回, 其余异常按选项决定是否暴露细节
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
            _ => new SnowRemoteServiceErrorInfo
            {
                Code = "InternalError",
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
