using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// SQL Server 提供程序配置扩展(对齐 ABP 的 <c>AbpDbContextOptionsSqlServerExtensions</c>)
/// </summary>
public static class SnowDbContextOptionsSqlServerExtensions
{
    /// <summary>
    /// 使用 SQL Server 作为数据库提供程序
    /// </summary>
    /// <param name="options">上下文配置集合</param>
    /// <param name="sqlServerOptionsAction">SQL Server 细项配置</param>
    public static void UseSqlServer(
        this SnowDbContextOptions options,
        Action<SqlServerDbContextOptionsBuilder>? sqlServerOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Configure(configurationContext =>
        {
            if (configurationContext.ExistingConnection is not null)
            {
                configurationContext.DbContextOptions.UseSqlServer(configurationContext.ExistingConnection, sqlServerOptionsAction);

                return;
            }

            string connectionString = configurationContext.ConnectionString
                                      ?? throw new InvalidOperationException($"未找到连接串 ConnectionStrings:{configurationContext.ConnectionStringName}。");

            configurationContext.DbContextOptions.UseSqlServer(connectionString, sqlServerOptionsAction);
        });
    }
}
