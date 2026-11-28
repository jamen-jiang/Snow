using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;

namespace Snow.Uow;

/// <summary>
/// 工作单元模块: 服务由 <see cref="UnitOfWorkAccessor"/> 与 <see cref="SnowUnitOfWorkManager"/>
/// 上的生命周期注解登记, 本模块负责挂上 <see cref="UnitOfWorkInterceptor"/> 的登记器
/// </summary>
public sealed class SnowUowModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 对齐 ABP: context.Services.OnRegistered(UnitOfWorkInterceptorRegistrar.RegisterIfNeeded)
        context.Services.OnRegistered(UnitOfWorkInterceptorRegistrar.RegisterIfNeeded);
    }
}
