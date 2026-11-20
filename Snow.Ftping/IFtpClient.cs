namespace Snow.Ftping;

/// <summary>
/// FTP 客户端接口
/// </summary>
public interface IFtpClient
{
    /// <summary>
    /// 获取 FTP 文件内容
    /// </summary>
    /// <param name="path">FTP 文件路径</param>
    /// <returns>文件内容(可空),文件不存在返回 null</returns>
    byte[]? Get(string path);

    /// <summary>
    /// 将流上传到 FTP 指定路径
    /// </summary>
    /// <param name="stream">要上传的流</param>
    /// <param name="remotePath">FTP 目标路径</param>
    /// <returns>上传是否成功</returns>
    bool Upload(Stream stream, string remotePath);

    /// <summary>
    /// 将字节数组上传到 FTP 指定路径
    /// </summary>
    /// <param name="buffer">要上传的字节数组</param>
    /// <param name="remotePath">FTP 目标路径</param>
    /// <returns>上传是否成功</returns>
    bool Upload(byte[] buffer, string remotePath);

    /// <summary>
    /// 将本地文件上传到 FTP 指定路径
    /// </summary>
    /// <param name="localPath">本地文件路径</param>
    /// <param name="remotePath">FTP 目标路径</param>
    /// <returns>上传是否成功</returns>
    bool Upload(string localPath, string remotePath);

    /// <summary>
    /// 删除 FTP 文件
    /// </summary>
    void Remove(string path);

    /// <summary>
    /// 创建 FTP 目录
    /// </summary>
    void CreateDirectory(string path);

    /// <summary>
    /// 判断 FTP 文件是否存在
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// 判断 FTP 目录是否存在
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// 获取 FTP 目录下的文件列表(只返回真文件:子目录、软链接、无法解析的条目一律剔除)
    /// </summary>
    /// <param name="path">FTP 目录路径</param>
    /// <returns>文件列表</returns>
    IReadOnlyList<FtpFile> GetFtpFiles(string path);
}
