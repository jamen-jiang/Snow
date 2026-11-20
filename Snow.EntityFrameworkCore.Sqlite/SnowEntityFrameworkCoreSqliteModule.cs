using Snow.Modularity;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// SQLite 提供程序模块: 只需随宿主加载, 配置由 <see cref="SnowDbContextOptionsSqliteExtensions.UseSqlite"/> 完成
/// </summary>
[DependsOn(typeof(SnowEntityFrameworkCoreModule))]
public sealed class SnowEntityFrameworkCoreSqliteModule : SnowModule
{
}
