using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// 选项工厂(对齐 ABP 的 <c>DbContextOptionsFactory</c>):
/// <para>解析连接串 → 依次执行预配置与配置动作 → 产出强类型选项, 供容器构造上下文。</para>
/// </summary>
public static class DbContextOptionsFactory
{
    /// <summary>
    /// 为指定上下文类型创建选项
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="dbContextType">上下文类型</param>
    /// <returns>选项对象(运行时类型为 <c>DbContextOptions&lt;TContext&gt;</c>)</returns>
    public static object Create(IServiceProvider serviceProvider, Type dbContextType)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(dbContextType);

        string connectionStringName = ConnectionStringNameAttribute.GetConnStringName(dbContextType);
        string? connectionString = ResolveConnectionString(serviceProvider, connectionStringName);

        Type builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
        var dbContextOptionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;

        ILoggerFactory? loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        if (loggerFactory is not null)
        {
            dbContextOptionsBuilder.UseLoggerFactory(loggerFactory);
        }

        dbContextOptionsBuilder.UseApplicationServiceProvider(serviceProvider);

        var configurationContext = new SnowDbContextConfigurationContext(
            connectionStringName,
            connectionString,
            serviceProvider,
            dbContextOptionsBuilder);

        SnowDbContextOptions options = serviceProvider.GetRequiredService<IOptions<SnowDbContextOptions>>().Value;

        foreach (Action<SnowDbContextConfigurationContext> preConfigureAction in options.DefaultPreConfigureActions)
        {
            preConfigureAction(configurationContext);
        }

        if (options.PreConfigureActions.TryGetValue(dbContextType, out List<Action<SnowDbContextConfigurationContext>>? preConfigureActions))
        {
            foreach (Action<SnowDbContextConfigurationContext> preConfigureAction in preConfigureActions)
            {
                preConfigureAction(configurationContext);
            }
        }

        if (options.ConfigureActions.TryGetValue(dbContextType, out Action<SnowDbContextConfigurationContext>? configureAction))
        {
            configureAction(configurationContext);
        }
        else if (options.DefaultConfigureAction is not null)
        {
            options.DefaultConfigureAction(configurationContext);
        }
        else
        {
            throw new InvalidOperationException(
                $"未找到 {dbContextType.FullName} 的数据库配置。请通过 services.Configure<SnowDbContextOptions>(options => options.Configure(...)) 或 options.UseSqlServer()/options.UseSqlite() 指定提供程序。");
        }

        return dbContextOptionsBuilder.Options;
    }

    /// <summary>
    /// 为指定上下文类型创建强类型选项
    /// </summary>
    /// <typeparam name="TDbContext">上下文类型</typeparam>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>强类型选项</returns>
    public static DbContextOptions<TDbContext> Create<TDbContext>(IServiceProvider serviceProvider)
        where TDbContext : DbContext
    {
        return (DbContextOptions<TDbContext>)Create(serviceProvider, typeof(TDbContext));
    }

    private static string? ResolveConnectionString(IServiceProvider serviceProvider, string connectionStringName)
    {
        IConfiguration? configuration = serviceProvider.GetService<IConfiguration>();

        return configuration?[$"ConnectionStrings:{connectionStringName}"]
               ?? configuration?["ConnectionStrings:Default"];
    }
}
