using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// PostgreSQL 提供程序配置扩展(对齐 ABP 的 <c>AbpDbContextOptionsPostgreSqlExtensions</c>)
/// </summary>
public static class SnowDbContextOptionsPostgreSqlExtensions
{
    /// <summary>
    /// 使用 PostgreSQL 作为数据库提供程序(命名对齐 EF Core 的 <c>UseNpgsql</c>)
    /// </summary>
    /// <param name="options">上下文配置集合</param>
    /// <param name="postgreSqlOptionsAction">PostgreSQL 细项配置</param>
    public static void UseNpgsql(
        this SnowDbContextOptions options,
        Action<NpgsqlDbContextOptionsBuilder>? postgreSqlOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Configure(configurationContext =>
        {
            if (configurationContext.ExistingConnection is not null)
            {
                configurationContext.DbContextOptions.UseNpgsql(configurationContext.ExistingConnection, postgreSqlOptionsAction);

                return;
            }

            string connectionString = configurationContext.ConnectionString
                                      ?? throw new InvalidOperationException($"未找到连接串 ConnectionStrings:{configurationContext.ConnectionStringName}。");

            configurationContext.DbContextOptions.UseNpgsql(connectionString, postgreSqlOptionsAction);
        });
    }
}
