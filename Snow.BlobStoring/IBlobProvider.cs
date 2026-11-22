namespace Snow.BlobStoring;

/// <summary>
/// 存储后端: 真正的读写实现(内存 / 文件系统 / 云对象存储)
/// </summary>
public interface IBlobProvider
{
    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="containerName">容器名</param>
    /// <param name="name">对象名</param>
    /// <param name="content">内容流</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(string containerName, string name, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取对象
    /// </summary>
    /// <param name="containerName">容器名</param>
    /// <param name="name">对象名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>内容流, 不存在为 null</returns>
    Task<Stream?> GetOrNullAsync(string containerName, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="containerName">容器名</param>
    /// <param name="name">对象名</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(string containerName, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否存在
    /// </summary>
    /// <param name="containerName">容器名</param>
    /// <param name="name">对象名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存在为 true</returns>
    Task<bool> ExistsAsync(string containerName, string name, CancellationToken cancellationToken = default);
}
