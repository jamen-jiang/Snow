namespace Rongia.Caching;

/// <summary>
/// 指定缓存项类型对应的逻辑缓存名(对应 ABP 的 CacheNameAttribute)。
/// <para>不标注时按类型全名去掉 <c>CacheItem</c> 后缀推导, 例如 <c>Acme.CompanyCacheItem</c> → <c>Acme.Company</c>。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public sealed class CacheNameAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">缓存名</param>
    public CacheNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }

    /// <summary>
    /// 缓存名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 取缓存项类型对应的逻辑缓存名
    /// </summary>
    /// <typeparam name="TCacheItem">缓存项类型</typeparam>
    /// <returns>缓存名</returns>
    public static string GetCacheName<TCacheItem>()
    {
        return GetCacheName(typeof(TCacheItem));
    }

    /// <summary>
    /// 取缓存项类型对应的逻辑缓存名: 优先读 <see cref="CacheNameAttribute"/>,
    /// 否则用类型全名去掉 <c>CacheItem</c> 后缀
    /// </summary>
    /// <param name="cacheItemType">缓存项类型</param>
    /// <returns>缓存名</returns>
    public static string GetCacheName(Type cacheItemType)
    {
        ArgumentNullException.ThrowIfNull(cacheItemType);

        CacheNameAttribute? attribute = cacheItemType.GetCustomAttributes(true).OfType<CacheNameAttribute>().FirstOrDefault();

        if (attribute is not null)
        {
            return attribute.Name;
        }

        string fullName = cacheItemType.FullName ?? cacheItemType.Name;
        const string suffix = "CacheItem";

        return fullName.EndsWith(suffix, StringComparison.Ordinal) ? fullName[..^suffix.Length] : fullName;
    }
}
