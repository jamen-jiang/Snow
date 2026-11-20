using Snow.Modularity;

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务契约模块: 空模块, 只作为模块图锚点
/// <para>没有它, 本程序集不会被约定扫描; 有了它, 实现包与调度框架适配包才有可 <c>[DependsOn]</c> 的目标,
/// 从而保证契约先入图、实现后替换的顺序。</para>
/// </summary>
public sealed class BackgroundJobsAbstractionsModule : SnowModule { }
