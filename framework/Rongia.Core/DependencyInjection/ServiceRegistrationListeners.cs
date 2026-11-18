using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Rongia.DependencyInjection;

/// <summary>
/// 服务登记监听表: 按服务集合实例存放监听器, 供约定注册在写入描述符时回调。
/// 用弱表挂载, 不往容器里塞标记类型的描述符, 容器解析不到任何多余的服务
/// </summary>
internal static class ServiceRegistrationListeners
{
    private static readonly ConditionalWeakTable<IServiceCollection, List<Action<OnServiceRegisteredContext>>> Listeners = new();

    /// <summary>
    /// 追加监听器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="listener">监听器</param>
    public static void Add(IServiceCollection services, Action<OnServiceRegisteredContext> listener)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(listener);

        List<Action<OnServiceRegisteredContext>> list = Listeners.GetOrCreateValue(services);

        lock (list)
        {
            list.Add(listener);
        }
    }

    /// <summary>
    /// 通知全部监听器; 未登记任何监听器时直接返回
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="descriptor">刚登记的描述符</param>
    public static void Notify(IServiceCollection services, ServiceDescriptor descriptor)
    {
        if (!Listeners.TryGetValue(services, out List<Action<OnServiceRegisteredContext>>? list))
        {
            return;
        }

        Action<OnServiceRegisteredContext>[] snapshot;

        lock (list)
        {
            if (list.Count == 0)
            {
                return;
            }

            snapshot = list.ToArray();
        }

        var context = new OnServiceRegisteredContext(services, descriptor);

        foreach (Action<OnServiceRegisteredContext> listener in snapshot)
        {
            listener(context);
        }
    }
}
