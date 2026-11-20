using Snow.Modularity;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// SQL Server 提供程序模块: 只需随宿主加载, 配置由 <see cref="SnowDbContextOptionsSqlServerExtensions.UseSqlServer"/> 完成
/// </summary>
[DependsOn(typeof(SnowEntityFrameworkCoreModule))]
public sealed class SnowEntityFrameworkCoreSqlServerModule : SnowModule
{
}
