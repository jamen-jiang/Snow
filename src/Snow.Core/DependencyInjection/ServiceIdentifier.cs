namespace Snow.DependencyInjection;

/// <summary>
/// 服务标识(对应 ABP 的 <c>ServiceIdentifier</c>): 把"服务类型 + 可选键"合成一个可做字典键的值,
/// 供 <see cref="CachedServiceProviderBase"/> 缓存解析结果
/// </summary>
public readonly struct ServiceIdentifier : IEquatable<ServiceIdentifier>
{
    /// <summary>
    /// 服务键, 不走键控服务时为 null
    /// </summary>
    public object? ServiceKey { get; }

    /// <summary>
    /// 服务类型
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// 用服务类型创建标识(不带键)
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    public ServiceIdentifier(Type serviceType)
    {
        ServiceType = serviceType;
    }

    /// <summary>
    /// 用服务键与服务类型创建标识
    /// </summary>
    /// <param name="serviceKey">服务键, null 表示不走键控服务</param>
    /// <param name="serviceType">服务类型</param>
    public ServiceIdentifier(object? serviceKey, Type serviceType)
    {
        ServiceKey = serviceKey;
        ServiceType = serviceType;
    }

    /// <summary>
    /// 比较两个标识是否指向同一个服务
    /// </summary>
    /// <param name="other">另一个标识</param>
    /// <returns>服务类型与键都相同时返回 <see langword="true"/></returns>
    public bool Equals(ServiceIdentifier other)
    {
        if (ServiceKey == null && other.ServiceKey == null)
        {
            return ServiceType == other.ServiceType;
        }

        if (ServiceKey != null && other.ServiceKey != null)
        {
            return ServiceType == other.ServiceType && ServiceKey.Equals(other.ServiceKey);
        }

        return false;
    }

    /// <summary>
    /// 比较两个标识是否指向同一个服务
    /// </summary>
    /// <param name="obj">另一个对象</param>
    /// <returns>是同一个服务时返回 <see langword="true"/></returns>
    public override bool Equals(object? obj)
    {
        return obj is ServiceIdentifier identifier && Equals(identifier);
    }

    /// <summary>
    /// 取哈希值: 有键时把键也掺进去, 无键时只用服务类型
    /// </summary>
    /// <returns>哈希值</returns>
    public override int GetHashCode()
    {
        if (ServiceKey == null)
        {
            return ServiceType.GetHashCode();
        }

        unchecked
        {
            return (ServiceType.GetHashCode() * 397) ^ ServiceKey.GetHashCode();
        }
    }
}
