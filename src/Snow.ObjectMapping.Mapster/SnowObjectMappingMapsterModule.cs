using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.ObjectMapping;

namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// Mapster 映射模块: 建一份 TypeAdapterConfig, 扫描模块程序集里的 IRegister 与 SnowAutoMapAttribute 并应用
/// <para>本模块存在即顶替 Snow.ObjectMapping 的静默实现, 不需要宿主再写任何一行。</para>
/// <para>宿主想额外加配置有两个口子: 把自己写的 <see cref="SnowMapsterProfile"/> 放进自己的程序集(会被扫描),
/// 或者在容器里登记 <c>IRegister</c>(<c>services.AddSingleton&lt;IRegister, MyRegister&gt;()</c>)。</para>
/// <para>对应 ABP 的 <c>AbpAutoMapperModule</c>, 只是引擎从 AutoMapper 换成 Mapster。</para>
/// </summary>
[DependsOn(typeof(SnowObjectMappingModule))]
public sealed class SnowObjectMappingMapsterModule : SnowModule
{
    /// <summary>
    /// 全局递归深度上限(与 ABP 的默认值一致), 防止自引用对象图无限递归
    /// </summary>
    private const int DefaultMaxDepth = 64;

    /// <summary>
    /// 登记配置与服务
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Assembly[] assemblies = [.. context.Services
            .GetSingletonInstance<IModuleContainer>()
            .Modules
            .SelectMany(module => module.AllAssemblies)
            .Distinct()];

        Type[] registerTypes = [.. assemblies
            .SelectMany(GetLoadableTypes)
            .Where(type => !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition && typeof(IRegister).IsAssignableFrom(type))
            .Distinct()];

        context.Services.TryAddSingleton(serviceProvider =>
        {
            TypeAdapterConfig config = new();
            HashSet<Type> applied = [];

            foreach (Type registerType in registerTypes)
            {
                if (applied.Add(registerType))
                {
                    ((IRegister)ActivatorUtilities.CreateInstance(serviceProvider, registerType)).Register(config);
                }
            }

            // 扫描之外的口子: 宿主自己登记到容器里的配置(同一类型不会重复应用)
            foreach (IRegister register in serviceProvider.GetServices<IRegister>())
            {
                if (applied.Add(register.GetType()))
                {
                    register.Register(config);
                }
            }

            ApplyAutoMaps(config, assemblies);
            config.Default.MaxDepth(DefaultMaxDepth);
            Compile(config);

            return config;
        });

        // 对应 ABP 里直接注入 AutoMapper 的 IMapper: Mapster 这边就是 MapsterMapper.IMapper
        context.Services.TryAddSingleton<IMapper>(serviceProvider => new Mapper(serviceProvider.GetRequiredService<TypeAdapterConfig>()));
    }

    /// <summary>
    /// 启动即编译全部映射: 写错的映射直接让应用起不来, 而不是等到第一次 Map 才炸
    /// </summary>
    private static void Compile(TypeAdapterConfig config)
    {
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
            foreach (Type type in GetLoadableTypes(assembly))
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
