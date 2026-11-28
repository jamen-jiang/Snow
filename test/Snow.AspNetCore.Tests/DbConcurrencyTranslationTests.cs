using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Snow.Data;
using Snow.EntityFrameworkCore;
using Snow.Exceptions;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 并发冲突翻译测试: SnowDbContext 的 SaveChanges/SaveChangesAsync 必须把 EF 的
/// DbUpdateConcurrencyException 统一翻译成 SnowDbConcurrencyException, 由异常处理层映射为 409
/// </summary>
public class DbConcurrencyTranslationTests
{
    [Fact]
    public void SaveChanges_ConcurrencyConflict_IsTranslated()
    {
        using SqliteConnection connection = new("DataSource=:memory:");
        connection.Open();
        DbContextOptions<ProbeDbContext> options = new DbContextOptionsBuilder<ProbeDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid id = Seed(options);

        using ProbeDbContext first = Create(options);
        using ProbeDbContext second = Create(options);
        ProbeEntity a = first.Probes.Single(x => x.Id == id);
        ProbeEntity b = second.Probes.Single(x => x.Id == id);
        a.Name = "from-first";
        a.Version = 2;
        b.Name = "from-second";
        b.Version = 2;

        first.SaveChanges();

        _ = Assert.Throws<SnowDbConcurrencyException>(() => second.SaveChanges());
    }

    [Fact]
    public async Task SaveChangesAsync_ConcurrencyConflict_IsTranslated()
    {
        using SqliteConnection connection = new("DataSource=:memory:");
        connection.Open();
        DbContextOptions<ProbeDbContext> options = new DbContextOptionsBuilder<ProbeDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid id = Seed(options);

        using ProbeDbContext first = Create(options);
        using ProbeDbContext second = Create(options);
        ProbeEntity a = first.Probes.Single(x => x.Id == id);
        ProbeEntity b = second.Probes.Single(x => x.Id == id);
        a.Name = "from-first";
        a.Version = 2;
        b.Name = "from-second";
        b.Version = 2;

        await first.SaveChangesAsync();

        _ = await Assert.ThrowsAsync<SnowDbConcurrencyException>(() => second.SaveChangesAsync());
    }

    private static Guid Seed(DbContextOptions<ProbeDbContext> options)
    {
        using ProbeDbContext setup = Create(options);
        setup.Database.EnsureCreated();
        ProbeEntity entity = new() { Id = Guid.NewGuid(), Name = "original", Version = 1 };
        _ = setup.Probes.Add(entity);
        _ = setup.SaveChanges();
        return entity.Id;
    }

    private static ProbeDbContext Create(DbContextOptions<ProbeDbContext> options)
    {
        ProbeDbContext context = new(options);

        // Initialize 是 internal: 测试直接反射调用, 传只返回 null 的容器(所有应用服务可空安全)
        MethodInfo initialize = typeof(SnowDbContext).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _ = initialize.Invoke(context, [new EmptyServiceProvider()]);
        return context;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : SnowDbContext(options)
    {
        public DbSet<ProbeEntity> Probes => Set<ProbeEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<ProbeEntity>().Property(x => x.Version).IsConcurrencyToken();
            base.OnModelCreating(modelBuilder);
        }
    }

    private sealed class ProbeEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Version { get; set; }
    }
}
