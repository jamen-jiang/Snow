namespace Snow.DependencyInjection;

/// <summary>
/// 服务登记动作清单(对应 ABP 的 <c>ServiceRegistrationActionList</c>):
/// 各模块在 ConfigureServices 里用 <c>services.OnRegistered(...)</c> 把自己的登记器挂进来。
/// </summary>
public class ServiceRegistrationActionList : List<Action<IOnServiceRegistredContext>>
{
    /// <summary>
    /// 是否关闭类代理(只做接口代理)
    /// </summary>
    public bool IsClassInterceptorsDisabled { get; set; }
}
