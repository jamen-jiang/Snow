using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Infrastructure;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// MySQL 提供程序配置扩展(对齐 ABP 的 <c>AbpDbContextOptionsMySQLExtensions</c>)
/// </summary>
public static class SnowDbContextOptionsMySqlExtensions
{
    /// <summary>
    /// 使用 MySQL 作为数据库提供程序
    /// </summary>
    /// <param name="options">上下文配置集合</param>
    /// <param name="mySqlOptionsAction">MySQL 细项配置</param>
    public static void UseMySql(
        this SnowDbContextOptions options,
        Action<MySQLDbContextOptionsBuilder>? mySqlOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Configure(configurationContext =>
        {
            if (configurationContext.ExistingConnection is not null)
            {
                configurationContext.DbContextOptions.UseMySQL(configurationContext.ExistingConnection, mySqlOptionsAction);

                return;
            }

            string connectionString = configurationContext.ConnectionString
                                      ?? throw new InvalidOperationException($"未找到连接串 ConnectionStrings:{configurationContext.ConnectionStringName}。");

            configurationContext.DbContextOptions.UseMySQL(connectionString, mySqlOptionsAction);
        });
    }
}
