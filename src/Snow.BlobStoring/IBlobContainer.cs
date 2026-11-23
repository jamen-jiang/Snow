namespace Snow.BlobStoring;

/// <summary>
/// 二进制对象容器: 按名字读写对象
/// <para>名字由调用方决定, 建议用 路径/文件名 形态组织层级。</para>
/// </summary>
public interface IBlobContainer
{
    /// <summary>
    /// 容器名
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 保存对象, 已存在则覆盖
    /// </summary>
    /// <param name="name">对象名</param>
    /// <param name="content">内容流</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(string name, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存对象(字节数组重载)
    /// </summary>
    /// <param name="name">对象名</param>
    /// <param name="bytes">内容字节</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(string name, byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取对象, 不存在抛异常
    /// </summary>
    /// <param name="name">对象名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>内容流</returns>
    Task<Stream> GetAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取对象, 不存在返回 null
    /// </summary>
    /// <param name="name">对象名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>内容流, 不存在为 null</returns>
    Task<Stream?> GetOrNullAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取对象全部字节
    /// </summary>
    /// <param name="name">对象名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>内容字节</returns>
    Task<byte[]> GetAllBytesAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除对象
    /// </summary>
    /// <param name="name">对象名</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 对象是否存在
    /// </summary>
    /// <param name="name">对象名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存在为 true</returns>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
