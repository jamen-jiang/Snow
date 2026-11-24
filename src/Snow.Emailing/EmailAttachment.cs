namespace Snow.Emailing;

/// <summary>
/// 邮件附件: 惰性打开内容流, 大附件不必先整个读进内存
/// </summary>
public sealed class EmailAttachment
{
    private readonly Func<Stream> _openStream;

    /// <summary>
    /// 构造器(内容由回调惰性提供)
    /// </summary>
    /// <param name="fileName">附件名(含扩展名, 用于推断内容类型)</param>
    /// <param name="openStream">打开内容流的回调, 每次调用返回一个可读流</param>
    /// <param name="contentType">内容类型; 为空则按扩展名推断</param>
    public EmailAttachment(string fileName, Func<Stream> openStream, string? contentType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(openStream);

        FileName = fileName;
        ContentType = contentType;
        _openStream = openStream;
    }

    /// <summary>
    /// 构造器(内容来自内存字节)
    /// </summary>
    /// <param name="fileName">附件名</param>
    /// <param name="data">内容</param>
    /// <param name="contentType">内容类型</param>
    public EmailAttachment(string fileName, byte[] data, string? contentType = null)
        : this(
            fileName,
            () => new MemoryStream(data ?? throw new ArgumentNullException(nameof(data)), writable: false),
            contentType)
    {
    }

    /// <summary>
    /// 附件名
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// 内容类型, 为空时由发送器按扩展名推断
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// 打开内容流, 调用方负责释放
    /// </summary>
    /// <returns>内容流</returns>
    public Stream OpenRead() => _openStream();
}
