using Snow.DependencyInjection;

namespace Snow.BlobStoring;

/// <summary>
/// 容器默认实现: 把容器名补上后转给 IBlobProvider
/// </summary>
/// <typeparam name="T">容器标记类型</typeparam>
[TransientService]
public sealed class BlobContainer<T> : IBlobContainer<T>
{
    private readonly IBlobProvider _provider;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="provider">存储后端</param>
    public BlobContainer(IBlobProvider provider)
    {
        _provider = provider;
        Name = BlobContainerNameResolver.Resolve(typeof(T));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task SaveAsync(string name, Stream content, CancellationToken cancellationToken = default)
        => _provider.SaveAsync(Name, name, content, cancellationToken);

    /// <inheritdoc />
    public async Task SaveAsync(string name, byte[] bytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using MemoryStream stream = new(bytes, writable: false);
        await _provider.SaveAsync(Name, name, stream, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Stream> GetAsync(string name, CancellationToken cancellationToken = default)
        => await GetOrNullAsync(name, cancellationToken).ConfigureAwait(false)
           ?? throw new FileNotFoundException("对象不存在: " + Name + "/" + name);

    /// <inheritdoc />
    public Task<Stream?> GetOrNullAsync(string name, CancellationToken cancellationToken = default)
        => _provider.GetOrNullAsync(Name, name, cancellationToken);

    /// <inheritdoc />
    public async Task<byte[]> GetAllBytesAsync(string name, CancellationToken cancellationToken = default)
    {
        await using Stream stream = await GetAsync(name, cancellationToken).ConfigureAwait(false);
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <inheritdoc />
    public Task DeleteAsync(string name, CancellationToken cancellationToken = default)
        => _provider.DeleteAsync(Name, name, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        => _provider.ExistsAsync(Name, name, cancellationToken);
}
