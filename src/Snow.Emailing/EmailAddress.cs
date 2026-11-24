using System.Net.Mail;

namespace Snow.Emailing;

/// <summary>
/// 邮件地址: 地址 + 可选显示名(值对象)
/// </summary>
/// <param name="Address">邮箱地址</param>
/// <param name="DisplayName">显示名</param>
public sealed record EmailAddress(string Address, string? DisplayName = null)
{
    /// <summary>
    /// 解析单个地址; 支持 "名字 &lt;a@b.com&gt;" 与纯地址
    /// </summary>
    /// <param name="value">地址文本</param>
    /// <returns>地址</returns>
    public static EmailAddress Parse(string value)
        => ParseList(value).FirstOrDefault() ?? throw new FormatException("邮件地址格式不正确: " + value);

    /// <summary>
    /// 解析地址列表; 支持逗号分隔多个地址与 "名字 &lt;a@b.com&gt;"
    /// </summary>
    /// <param name="value">地址文本</param>
    /// <returns>地址集合</returns>
    public static IReadOnlyList<EmailAddress> ParseList(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        MailAddressCollection collection = new();
        try
        {
            collection.Add(value);
        }
        catch (FormatException ex)
        {
            throw new FormatException("邮件地址格式不正确: " + value, ex);
        }

        return collection
            .Select(address => new EmailAddress(
                address.Address,
                string.IsNullOrWhiteSpace(address.DisplayName) ? null : address.DisplayName))
            .ToArray();
    }

    /// <inheritdoc />
    public override string ToString() => DisplayName is null ? Address : DisplayName + " <" + Address + ">";
}
