using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.Modularity;

namespace Snow.Uow;

/// <summary>
/// 工作单元模块: 登记环境访问器与默认管理器
/// </summary>
public sealed class SnowUowModule : SnowModule
{
    /// <summary>
    /// 登记服务
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.TryAddSingleton<IUnitOfWorkAccessor, UnitOfWorkAccessor>();
        context.Services.TryAddTransient<IUnitOfWorkManager, SnowUnitOfWorkManager>();
    }
}
