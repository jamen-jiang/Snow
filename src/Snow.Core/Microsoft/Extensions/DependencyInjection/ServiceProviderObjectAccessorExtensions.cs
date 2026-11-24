using System;
using Snow.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 已构建容器(<see cref="IServiceProvider"/>)上的对象访问器扩展(Snow 自研; ABP 只提供基于 <see cref="IServiceCollection"/> 的版本)。
/// <para>运行期(容器已构建)要取宿主对象时, Snow 需要基于 IServiceProvider 的版本,
/// 供 Snow.AspNetCore 的应用初始化上下文扩展(GetApplicationBuilder 等)使用。</para>
/// </summary>
public static class ServiceProviderObjectAccessorExtensions
{
    /// <summary>
    /// 从容器取对象访问器里当前的值: 没登记访问器、或登记了还没填值, 都返回 null。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>对象实例或 null</returns>
    public static T? GetObjectOrNull<T>(this IServiceProvider serviceProvider)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider.GetService<IObjectAccessor<T>>()?.Value;
    }

    /// <summary>
    /// 从容器取对象访问器里当前的值, 取不到抛异常。
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>对象实例</returns>
    /// <exception cref="InvalidOperationException">尚未用 AddObjectAccessor 登记该类型</exception>
    public static T GetObject<T>(this IServiceProvider serviceProvider)
        where T : class
    {
        return serviceProvider.GetObjectOrNull<T>()
            ?? throw new InvalidOperationException("Could not find an object of " + typeof(T).AssemblyQualifiedName + " in service provider. Be sure that you have used AddObjectAccessor before!");
    }
}
