using Microsoft.Extensions.DependencyInjection;

namespace Rongia.DependencyInjection;

/// <summary>
/// 服务登记事件上下文: 约定注册往容器里写入一个描述符时, 把该描述符交给监听者
/// </summary>
public sealed class OnServiceRegisteredContext
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="descriptor">刚刚登记的描述符</param>
    public OnServiceRegisteredContext(IServiceCollection services, ServiceDescriptor descriptor)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    /// <summary>
    /// 服务集合
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// 刚刚登记的描述符
    /// </summary>
    public ServiceDescriptor Descriptor { get; }

    /// <summary>
    /// 实现类型: 使用工厂或实例登记时退化为服务类型本身
    /// </summary>
    public Type ImplementationType => Descriptor.ImplementationType ?? Descriptor.ServiceType;
}
