using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// DbContext 配置上下文(对齐 ABP 的 <c>AbpDbContextConfigurationContext</c>):
/// <para>把解析出的连接串、服务提供者与选项构建器一并交给配置动作。</para>
/// </summary>
public class SnowDbContextConfigurationContext
{
    /// <summary>
    /// 连接串名称(ConnectionStrings 节下的键名)
    /// </summary>
    public string? ConnectionStringName { get; }

    /// <summary>
    /// 解析出的连接串
    /// </summary>
    public string? ConnectionString { get; }

    /// <summary>
    /// 当前服务提供者
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 已有连接(事务内共享连接时使用)
    /// </summary>
    public DbConnection? ExistingConnection { get; }

    /// <summary>
    /// 选项构建器
    /// </summary>
    public DbContextOptionsBuilder DbContextOptions { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="connectionStringName">连接串名称</param>
    /// <param name="connectionString">连接串</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="dbContextOptions">选项构建器</param>
    /// <param name="existingConnection">已有连接</param>
    public SnowDbContextConfigurationContext(
        string? connectionStringName,
        string? connectionString,
        IServiceProvider serviceProvider,
        DbContextOptionsBuilder dbContextOptions,
        DbConnection? existingConnection = null)
    {
        ConnectionStringName = connectionStringName;
        ConnectionString = connectionString;
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        DbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
        ExistingConnection = existingConnection;
    }
}

/// <summary>
/// 带上下文类型的配置上下文: 把通用的选项构建器暴露成强类型版本
/// </summary>
/// <typeparam name="TDbContext">上下文类型</typeparam>
public sealed class SnowDbContextConfigurationContext<TDbContext> : SnowDbContextConfigurationContext
    where TDbContext : DbContext
{
    /// <summary>
    /// 强类型选项构建器(直接把基类的通用构建器强转过来, 不额外存字段)
    /// </summary>
    public new DbContextOptionsBuilder<TDbContext> DbContextOptions
        => (DbContextOptionsBuilder<TDbContext>)base.DbContextOptions;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="context">通用配置上下文</param>
    public SnowDbContextConfigurationContext(SnowDbContextConfigurationContext context)
        : base(
            context.ConnectionStringName,
            context.ConnectionString,
            context.ServiceProvider,
            context.DbContextOptions,
            context.ExistingConnection)
    {
    }
}
