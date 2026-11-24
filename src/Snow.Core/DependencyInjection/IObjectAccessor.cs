namespace Snow.DependencyInjection;

/// <summary>
/// 对象访问器契约(对应 ABP 的 IObjectAccessor):
/// 让单例服务在运行期取到只有宿主才有的对象(应用构建器、宿主、端点路由等)
/// </summary>
/// <typeparam name="T">对象类型</typeparam>
public interface IObjectAccessor<T>
{
    /// <summary>
    /// 当前对象, 宿主还没填时是 null
    /// </summary>
    T? Value { get; set; }
}
