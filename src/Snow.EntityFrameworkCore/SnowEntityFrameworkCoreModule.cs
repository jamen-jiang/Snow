using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snow.Data;
using Snow.DependencyInjection;
using Snow.Domain;
using Snow.Domain.Repositories;
using Snow.Domain.Repositories.EntityFrameworkCore;
using Snow.Modularity;
using Snow.Uow;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// EF Core 模块: 登记默认选项、DbContext 提供器, 并按模块程序集扫描把上下文与仓储注册进容器
/// </summary>
[DependsOn(typeof(SnowUowModule))]
[DependsOn(typeof(SnowDataModule))]
[DependsOn(typeof(SnowDomainModule))]
public sealed class SnowEntityFrameworkCoreModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Configure<SnowDbContextOptions>(options =>
        {
            options.PreConfigure(configurationContext =>
            {
                configurationContext.DbContextOptions.ConfigureWarnings(warnings =>
                    warnings.Ignore(CoreEventId.LazyLoadOnDisposedContextWarning));
            });
        });

        IModuleContainer container = context.Services.GetSingletonInstance<IModuleContainer>();

        Assembly[] assemblies = container.Modules
            .SelectMany(module => module.AllAssemblies)
            .Distinct()
            .ToArray();

        foreach (Type dbContextType in FindDbContextTypes(assemblies))
        {
            RegisterDbContext(context.Services, dbContextType);
        }
    }

    private static IEnumerable<Type> FindDbContextTypes(IEnumerable<Assembly> assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in AssemblyTypeCache.GetTypes(assembly))
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                if (typeof(DbContext).IsAssignableFrom(type))
                {
                    yield return type;
                }
            }
        }
    }

    private static void RegisterDbContext(IServiceCollection services, Type dbContextType)
    {
        // 容器负责创建与释放; 工厂里现算选项, 使连接串在解析时才生效
        services.TryAddScoped(dbContextType, serviceProvider =>
        {
            object options = DbContextOptionsFactory.Create(serviceProvider, dbContextType);
            object instance = ActivatorUtilities.CreateInstance(serviceProvider, dbContextType, options);

            // 把应用服务提供者交给上下文: 审计要取 ICurrentUser, 领域事件要取 IEventBus
            if (instance is SnowDbContext snowDbContext)
            {
                snowDbContext.ApplicationServiceProvider = serviceProvider;
            }

            return instance;
        });

        foreach (Type entityType in GetEntityTypes(dbContextType))
        {
            Type repositoryInterface = typeof(IRepository<>).MakeGenericType(entityType);
            Type repositoryImplementation = typeof(EfCoreRepository<,>).MakeGenericType(dbContextType, entityType);

            services.TryAddScoped(repositoryInterface, repositoryImplementation);
        }
    }

    private static IEnumerable<Type> GetEntityTypes(Type dbContextType)
    {
        return dbContextType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType.IsGenericType &&
                               property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => property.PropertyType.GetGenericArguments()[0])
            .Distinct();
    }
}
