namespace Snow.Ftping;

/// <summary>
/// FTP 文件(只表示真文件:目录、软链接、解析失败的条目在取列表时已被剔除)
/// </summary>
public class FtpFile
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 完整路径(父目录 + 文件名,可直接喂给 Get/Remove)
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// 文件大小(字节,可空:服务器未返回大小时为 null)
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// 修改时间(可空:服务器未返回时间时为 null;部分服务器精度只到分钟)
    /// </summary>
    public DateTime? ModifiedTime { get; set; }
}
