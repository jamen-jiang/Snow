namespace Snow.Ftping;

/// <summary>
/// FTP 目录项类型
/// </summary>
public enum FtpEntryType
{
    /// <summary>
    /// 文件
    /// </summary>
    File,

    /// <summary>
    /// 目录
    /// </summary>
    Directory,

    /// <summary>
    /// 软链接
    /// </summary>
    Link
}

/// <summary>
/// FTP 目录项(文件 / 目录 / 软链接)
/// </summary>
public sealed class FtpEntry
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 完整路径(父目录 + 名称, 可直接喂给各操作方法)
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// 类型
    /// </summary>
    public FtpEntryType Type { get; set; }

    /// <summary>
    /// 大小(字节, 可空: 服务器未返回或为目录时为 null)
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// 修改时间(可空: 服务器未返回时为 null)
    /// </summary>
    public System.DateTime? ModifiedTime { get; set; }
}
