using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Validation.FluentValidation;

/// <summary>
/// FluentValidation 集成模块: 扫描并登记校验器, 把校验管线换成 DataAnnotations + FluentValidation 的合成实现
/// <para>依赖 <see cref="SnowValidationModule"/>: 合成实现要复用它的 DataAnnotations 校验器。</para>
/// </summary>
[DependsOn(typeof(SnowValidationModule))]
public sealed class SnowValidationFluentValidationModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SnowFluentValidationOptions options = new();
        IConfigurationSection section = context.Configuration.GetSection("Validation:FluentValidation");

        if (section.Exists())
        {
            section.Bind(options);
        }

        SnowFluentValidationOptionsSetup? setup = context.Services
            .Where(descriptor => descriptor.ServiceType == typeof(SnowFluentValidationOptionsSetup))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<SnowFluentValidationOptionsSetup>()
            .FirstOrDefault();

        setup?.Apply(options);

        if (!options.Enabled)
        {
            return;
        }

        context.Services.AddSingleton(options);

        if (options.ScanValidators)
        {
            foreach (Assembly assembly in ResolveAssemblies(context))
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (IsValidator(type) && !type.IsDefined(typeof(DisableConventionalRegistrationAttribute), inherit: true))
                    {
                        RegisterValidator(context.Services, type);
                    }
                }
            }
        }

        foreach (Type validatorType in options.ValidatorTypes)
        {
            RegisterValidator(context.Services, validatorType);
        }

        context.Services.Replace(ServiceDescriptor.Singleton<ISnowObjectValidator, FluentValidationObjectValidator>());
    }

    private static IReadOnlyList<Assembly> ResolveAssemblies(ServiceConfigurationContext context)
    {
        HashSet<Assembly> assemblies = [];

        IModuleContainer? container = context.Services.GetSingletonInstanceOrNull<IModuleContainer>();
        if (container is not null)
        {
            foreach (SnowModuleDescriptor module in container.Modules)
            {
                assemblies.Add(module.Type.Assembly);
            }
        }

        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            assemblies.Add(entryAssembly);
        }

        return assemblies.ToList();
    }

    // 对应 ABP 的 AbpFluentValidationConventionalRegistrar: 瞬态登记, 同时暴露具体类型与 IValidator<T>
    private static void RegisterValidator(IServiceCollection services, Type validatorType)
    {
        if (validatorType.IsAbstract || validatorType.IsGenericTypeDefinition)
        {
            return;
        }

        services.TryAdd(ServiceDescriptor.Transient(validatorType, validatorType));

        foreach (Type contract in validatorType.GetInterfaces())
        {
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IValidator<>))
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(contract, validatorType));
            }
        }
    }

    private static bool IsValidator(Type type)
    {
        return type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
               type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IValidator<>));
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
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
