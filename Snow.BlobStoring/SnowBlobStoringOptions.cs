namespace Snow.BlobStoring;

/// <summary>
/// 对象存储选项
/// <para>目前只有存储后端类型; 容器名由调用方类型决定, 不需要预先登记。</para>
/// </summary>
public sealed class SnowBlobStoringOptions
{
    /// <summary>
    /// 存储后端类型, 默认 InMemoryBlobProvider
    /// </summary>
    public Type ProviderType { get; set; } = typeof(InMemoryBlobProvider);
}
