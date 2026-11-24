namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="ServiceDescriptor"/> 扩展(对应 ABP 的 <c>ServiceDescriptorExtensions</c>):
/// 在键控服务与非键控服务之间归一化实现实例 / 实现类型。
/// </summary>
public static class ServiceDescriptorExtensions
{
    /// <summary>
    /// 取归一化后的实现实例。
    /// </summary>
    public static object? NormalizedImplementationInstance(this ServiceDescriptor descriptor)
        => descriptor.IsKeyedService ? descriptor.KeyedImplementationInstance : descriptor.ImplementationInstance;

    /// <summary>
    /// 取归一化后的实现类型。
    /// </summary>
    public static Type? NormalizedImplementationType(this ServiceDescriptor descriptor)
        => descriptor.IsKeyedService ? descriptor.KeyedImplementationType : descriptor.ImplementationType;
}
