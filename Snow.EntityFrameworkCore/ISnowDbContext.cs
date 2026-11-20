using Microsoft.EntityFrameworkCore;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// Snow 的 EF Core 上下文标记(对齐 ABP 的 <c>IEfCoreDbContext</c>):
/// <para>仓储与提供器只面向本接口编程, 使用者继承 <see cref="SnowDbContext"/> 即自动满足约束。</para>
/// </summary>
public interface ISnowDbContext
{
    /// <summary>
    /// 底层 EF Core 上下文实例
    /// </summary>
    DbContext DbContext { get; }
}
