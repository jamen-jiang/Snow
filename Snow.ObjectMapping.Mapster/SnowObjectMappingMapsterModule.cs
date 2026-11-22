using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.ObjectMapping;

namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// Mapster 映射模块: 建一份 TypeAdapterConfig, 扫描模块程序集里的 IRegister 与 SnowAutoMapAttribute 并应用
/// <para>本模块存在即顶替 Snow.ObjectMapping 的静默实现, 不需要宿主再写任何一行。</para>
/// <para>对应 ABP 的 <c>AbpAutoMapperModule</c>, 只是引擎从 AutoMapper 换成 Mapster。</para>
/// </summary>
[DependsOn(typeof(SnowObjectMappingModule))]
public sealed class SnowObjectMappingMapsterModule : SnowModule
{
    /// <summary>
    /// 登记配置与服务
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        SnowObjectMappingMapsterOptions options = new();
        IConfigurationSection section = context.Services.GetConfiguration().GetSection("ObjectMapping:Mapster");

        if (section.Exists())
        {
            section.Bind(options);
        }

        SnowObjectMappingMapsterOptionsSetup? setup = context.Services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowObjectMappingMapsterOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowObjectMappingMapsterOptionsSetup>()
            .FirstOrDefault();

        setup?.Apply(options);

        if (!options.Enabled)
        {
            return;
        }

        context.Services.AddSingleton(options);
        context.Services.Configure<SnowObjectMappingMapsterOptions>(configured =>
        {
            configured.Enabled = options.Enabled;
            configured.CompileMappings = options.CompileMappings;
            configured.ValidateMappings = options.ValidateMappings;
            configured.ScanRegisters = options.ScanRegisters;
            configured.ScanAutoMaps = options.ScanAutoMaps;
            configured.DefaultMaxDepth = options.DefaultMaxDepth;
            configured.Configure = options.Configure;

            foreach (Type registerType in options.RegisterTypes)
            {
                configured.RegisterTypes.Add(registerType);
            }

            foreach (Type profileType in options.ProfileTypes)
            {
                configured.ProfileTypes.Add(profileType);
            }

            foreach (Assembly assembly in options.MapAssemblies)
            {
                configured.MapAssemblies.Add(assembly);
            }

            foreach (Action<TypeAdapterConfig> configurator in options.Configurators)
            {
                configured.Configurators.Add(configurator);
            }
        });

        IReadOnlyList<Type> registerTypes = ResolveRegisterTypes(context, options);
        IReadOnlyList<Assembly> scannedAssemblies = ResolveScannedAssemblies(context, options);

        context.Services.TryAddSingleton(serviceProvider =>
        {
            TypeAdapterConfig config = new();

            foreach (Type registerType in registerTypes)
            {
                IRegister register = (IRegister)ActivatorUtilities.CreateInstance(serviceProvider, registerType);
                register.Register(config);
            }

            if (options.ScanAutoMaps)
            {
                ApplyAutoMaps(config, scannedAssemblies);
            }

            if (options.DefaultMaxDepth.HasValue)
            {
                config.Default.MaxDepth(options.DefaultMaxDepth.Value);
            }

            foreach (Action<TypeAdapterConfig> configurator in options.Configurators)
            {
                configurator(config);
            }

            options.Configure?.Invoke(config);

            if (options.CompileMappings || options.ValidateMappings)
            {
                Compile(config, options.ValidateMappings);
            }

            return config;
        });

        // 对应 ABP 里直接注入 AutoMapper 的 IMapper: Mapster 这边就是 MapsterMapper.IMapper
        context.Services.TryAddSingleton<IMapper>(serviceProvider =>
        {
            TypeAdapterConfig config = serviceProvider.GetRequiredService<TypeAdapterConfig>();

            return new Mapper(config);
        });
    }

    private static void Compile(TypeAdapterConfig config, bool validate)
    {
        if (!validate)
        {
            config.Compile();

            return;
        }

        try
        {
            config.Compile();
        }
        catch (Exception exception)
        {
            throw new SnowObjectMappingException("映射配置校验失败: " + exception.Message, exception);
        }
    }

    private static void ApplyAutoMaps(TypeAdapterConfig config, IEnumerable<Assembly> assemblies)
    {
        HashSet<(Type Source, Type Destination)> registered = [];

        foreach (Assembly assembly in assemblies)
        {
            Type[] types;

            try
            {
                types = assembly.GetExportedTypes();
            }
            catch (Exception)
            {
                continue;
            }

            foreach (Type type in types)
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                foreach (SnowAutoMapAttribute attribute in type.GetCustomAttributes<SnowAutoMapAttribute>())
                {
                    foreach (Type otherType in attribute.Types)
                    {
                        switch (attribute.Direction)
                        {
                            case SnowAutoMapDirection.To:
                                AddMap(config, registered, type, otherType);
                                break;

                            case SnowAutoMapDirection.From:
                                AddMap(config, registered, otherType, type);
                                break;

                            case SnowAutoMapDirection.TwoWays:
                                AddMap(config, registered, type, otherType);
                                AddMap(config, registered, otherType, type);
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
        }
    }

    private static void AddMap(TypeAdapterConfig config, HashSet<(Type Source, Type Destination)> registered, Type sourceType, Type destinationType)
    {
        if (sourceType == destinationType || sourceType.IsGenericTypeDefinition || destinationType.IsGenericTypeDefinition)
        {
            return;
        }

        if (!registered.Add((sourceType, destinationType)))
        {
            return;
        }

        config.NewConfig(sourceType, destinationType);
    }

    private static IReadOnlyList<Type> ResolveRegisterTypes(ServiceConfigurationContext context, SnowObjectMappingMapsterOptions options)
    {
        List<Type> types = [];

        foreach (Assembly assembly in ResolveRegisterAssemblies(context, options))
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                if (typeof(IRegister).IsAssignableFrom(type) && !types.Contains(type))
                {
                    types.Add(type);
                }
            }
        }

        foreach (Type registerType in options.RegisterTypes)
        {
            if (!types.Contains(registerType))
            {
                types.Add(registerType);
            }
        }

        foreach (Type profileType in options.ProfileTypes)
        {
            if (!typeof(SnowMapsterProfile).IsAssignableFrom(profileType))
            {
                throw new SnowObjectMappingException($"类型 {profileType.FullName} 不是 {nameof(SnowMapsterProfile)} 的子类, 不能当映射配置登记。");
            }

            if (!types.Contains(profileType))
            {
                types.Add(profileType);
            }
        }

        return types;
    }

    private static IReadOnlyList<Assembly> ResolveRegisterAssemblies(ServiceConfigurationContext context, SnowObjectMappingMapsterOptions options)
    {
        List<Assembly> assemblies = [];

        if (options.ScanRegisters)
        {
            assemblies.AddRange(context.Services
                .GetSingletonInstance<IModuleContainer>()
                .Modules
                .SelectMany(module => module.AllAssemblies)
                .Distinct());
        }

        foreach (Assembly assembly in options.MapAssemblies)
        {
            if (!assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
            }
        }

        return assemblies;
    }

    private static IReadOnlyList<Assembly> ResolveScannedAssemblies(ServiceConfigurationContext context, SnowObjectMappingMapsterOptions options)
    {
        List<Assembly> assemblies = [];

        if (options.ScanRegisters || options.ScanAutoMaps)
        {
            assemblies.AddRange(context.Services
                .GetSingletonInstance<IModuleContainer>()
                .Modules
                .SelectMany(module => module.AllAssemblies)
                .Distinct());
        }

        foreach (Assembly assembly in options.MapAssemblies)
        {
            if (!assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
            }
        }

        return assemblies;
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (Exception)
        {
            return [];
        }
    }
}
