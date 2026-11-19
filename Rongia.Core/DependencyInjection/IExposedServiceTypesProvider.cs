namespace Rongia.DependencyInjection;

/// <summary>
/// 暴露服务类型提供者:约定注册时用它决定某个实现类型要以哪些服务类型登记(对应 ABP 同名接口)
/// </summary>
public interface IExposedServiceTypesProvider
{
    /// <summary>
    /// 获取目标类型应对外暴露的服务类型
    /// </summary>
    /// <param name="targetType">实现类型</param>
    /// <returns>服务类型集合</returns>
    Type[] GetExposedServiceTypes(Type targetType);
}
