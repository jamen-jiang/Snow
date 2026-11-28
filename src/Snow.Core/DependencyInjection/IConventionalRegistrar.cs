using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.DependencyInjection;

/// <summary>
/// 约定登记器(对应 ABP 的 <c>IConventionalRegistrar</c>): 让框架包把「自己扫描出来的类型」交给约定注册管道,
/// 与标了 <see cref="ServiceAttribute"/> 的类型走同一套登记与拦截器处理, 业务代码因此不用写服务注解。
/// <para>Snow 的约定注册分「收集候选 → 统一写入」两步: 这里的 Add / AddTypes / AddType 只是把类型记成候选,
/// 真正的服务描述符在统一写入阶段产生; 语义与 ABP 在登记时刻直接写描述符等价。</para>
/// </summary>
public interface IConventionalRegistrar
{
    /// <summary>
    /// 把程序集里符合本登记器范围的类型登记为约定候选
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assembly">待扫描程序集</param>
    void AddAssembly(IServiceCollection services, Assembly assembly);

    /// <summary>
    /// 把一批类型登记为约定候选
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="types">类型数组</param>
    void AddTypes(IServiceCollection services, params Type[] types);

    /// <summary>
    /// 把单个类型登记为约定候选; 不在本登记器范围时静默跳过
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="type">实现类型</param>
    void AddType(IServiceCollection services, Type type);
}
