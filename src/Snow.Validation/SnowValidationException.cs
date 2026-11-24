using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Snow.Exceptions;

namespace Snow.Validation;

/// <summary>
/// 参数校验失败异常: 携带字段级错误明细
/// <para>业务层与接口层都抛它, 由异常处理包翻译成 400 与字段错误列表。</para>
/// </summary>
public class SnowValidationException : SnowException
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
        LogLevel = LogLevel.Warning;
    }

    /// <summary>
    /// 校验错误列表
    /// </summary>
    public IReadOnlyList<ValidationResult> ValidationErrors { get; }

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