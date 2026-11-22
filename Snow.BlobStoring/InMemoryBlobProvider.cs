using System.Collections.Concurrent;
using Snow.DependencyInjection;

namespace Snow.BlobStoring;

/// <summary>
/// 内存存储后端: 进程内字典保存, 进程退出即丢失, 适合开发与测试
/// </summary>
[SingletonService]
public sealed class InMemoryBlobProvider : IBlobProvider
{
    private readonly ConcurrentDictionary<string, byte[]> _blobs = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public async Task SaveAsync(string containerName, string name, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        _blobs[Key(containerName, name)] = buffer.ToArray();
    }

    /// <inheritdoc />
    public Task<Stream?> GetOrNullAsync(string containerName, string name, CancellationToken cancellationToken = default)
    {
        if (!_blobs.TryGetValue(Key(containerName, name), out byte[]? bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(bytes, writable: false));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string containerName, string name, CancellationToken cancellationToken = default)
    {
        _blobs.TryRemove(Key(containerName, name), out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string containerName, string name, CancellationToken cancellationToken = default)
        => Task.FromResult(_blobs.ContainsKey(Key(containerName, name)));

    private static string Key(string containerName, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return containerName + "/" + name;
    }
}
