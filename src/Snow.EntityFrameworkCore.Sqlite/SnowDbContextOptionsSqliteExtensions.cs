using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// SQLite 提供程序配置扩展(对齐 ABP 的 <c>AbpDbContextOptionsSqliteExtensions</c>)
/// </summary>
public static class SnowDbContextOptionsSqliteExtensions
{
    /// <summary>
    /// 使用 SQLite 作为数据库提供程序
    /// </summary>
    /// <param name="options">上下文配置集合</param>
    /// <param name="sqliteOptionsAction">SQLite 细项配置</param>
    public static void UseSqlite(
        this SnowDbContextOptions options,
        Action<SqliteDbContextOptionsBuilder>? sqliteOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Configure(configurationContext =>
        {
            if (configurationContext.ExistingConnection is not null)
            {
                configurationContext.DbContextOptions.UseSqlite(configurationContext.ExistingConnection, sqliteOptionsAction);

                return;
            }

            string connectionString = configurationContext.ConnectionString
                                      ?? throw new InvalidOperationException($"未找到连接串 ConnectionStrings:{configurationContext.ConnectionStringName}。");

            configurationContext.DbContextOptions.UseSqlite(connectionString, sqliteOptionsAction);
        });
    }
}
