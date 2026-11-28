using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.DependencyInjection;

/// <summary>
/// 外部登记器送进来的约定候选: 实现类型、生命周期、暴露的服务类型、是否替换既有登记
/// </summary>
/// <param name="ImplementationType">实现类型</param>
/// <param name="Lifetime">生命周期</param>
/// <param name="ServiceTypes">暴露的服务类型</param>
/// <param name="ReplaceServices">是否替换既有登记</param>
internal readonly record struct ExternalConventionalRegistration(
    Type ImplementationType,
    ServiceLifetime Lifetime,
    Type[] ServiceTypes,
    bool ReplaceServices);

/// <summary>
/// 外部约定候选清单: 挂在服务集合上的单例, 统一写入阶段一次性取出
/// </summary>
internal sealed class ExternalConventionalRegistrationList : List<ExternalConventionalRegistration>
{
}

/// <summary>
/// 约定登记器清单: 挂在服务集合上的单例
/// </summary>
internal sealed class ConventionalRegistrarList : List<IConventionalRegistrar>
{
}
