using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Snow.Exceptions;
using Snow.Logging;

namespace Snow.Validation;

/// <summary>
/// 参数校验失败异常: 携带字段级错误明细
/// <para>业务层与接口层都抛它, 由异常处理包翻译成 400 与字段错误列表。</para>
/// </summary>
public class SnowValidationException : SnowException, IHasValidationErrors, IHasLogLevel
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="validationErrors">校验错误列表</param>
    /// <param name="message">异常消息</param>
    public SnowValidationException(IReadOnlyList<ValidationResult> validationErrors, string? message = null)
        : base(message ?? BuildMessage(validationErrors))
    {
        ValidationErrors = validationErrors ?? Array.Empty<ValidationResult>();
    }

    /// <summary>
    /// 校验错误列表
    /// </summary>
    public IReadOnlyList<ValidationResult> ValidationErrors { get; }

    /// <summary>
    /// 日志级别: 校验失败是预期内的, 记 Warning
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    private static string BuildMessage(IReadOnlyList<ValidationResult>? errors)
    {
        if (errors is null || errors.Count == 0)
        {
            return "参数校验失败。";
        }

        IEnumerable<string> messages = errors
            .Select(error => string.Join(", ", error.MemberNames.DefaultIfEmpty(string.Empty)) + ": " + error.ErrorMessage)
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return "参数校验失败: " + string.Join("; ", messages);
    }
}