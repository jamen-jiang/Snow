namespace Snow.EntityFrameworkCore;

/// <summary>
/// DbContext 提供器(对齐 ABP 的 <c>IDbContextProvider</c>):
/// <para>同一服务作用域内反复取到同一个上下文实例, 由容器负责释放。</para>
/// </summary>
/// <typeparam name="TDbContext">上下文类型</typeparam>
public interface IDbContextProvider<TDbContext>
    where TDbContext : ISnowDbContext
{
    /// <summary>
    /// 取当前作用域内的上下文
    /// </summary>
    /// <returns>上下文实例</returns>
    Task<TDbContext> GetDbContextAsync();
}
