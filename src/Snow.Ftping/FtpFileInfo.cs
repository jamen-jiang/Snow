namespace Snow.Ftping;

/// <summary>
/// 单个 FTP 文件的元数据(仅大小与修改时间, 不含内容)
/// </summary>
public sealed class FtpFileInfo
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 完整路径
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    /// 大小(字节, 可空: 服务器未返回大小时为 null)
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// 修改时间(可空: 服务器未返回时间时为 null; 部分服务器精度只到分钟)
    /// </summary>
    public System.DateTime? ModifiedTime { get; set; }
}
