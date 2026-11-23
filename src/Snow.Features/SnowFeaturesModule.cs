using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Features;

/// <summary>
/// 功能开关模块(对应 ABP 的 <c>AbpFeaturesModule</c>)
/// <para>定义管理器 / 值提供器 / 检查入口靠生命周期注解参与约定注册;
/// 这里额外把各模块里的 <see cref="FeatureDefinitionProvider"/> 子类按基类型登记, 否则
/// 管理器拿不到它们(约定注册只会按接口或自身登记, 不会按抽象基类登记)。</para>
/// </summary>
public sealed class SnowFeaturesModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IModuleContainer container = context.Services.GetSingletonInstance<IModuleContainer>();
        IEnumerable<Assembly> assemblies = container.Modules.SelectMany(module => module.AllAssemblies).Distinct();

        foreach (Type providerType in assemblies
                     .SelectMany(assembly => assembly.GetExportedTypes())
                     .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(FeatureDefinitionProvider).IsAssignableFrom(type)))
        {
            context.Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(FeatureDefinitionProvider), providerType));
        }
    }
}
