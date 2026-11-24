using System;

namespace Snow;

/// <summary>
/// 约定控制器要收集哪一类应用服务(对应 ABP 的 <c>ApplicationServiceTypes</c>)。
/// </summary>
[Flags]
public enum ApplicationServiceTypes : byte
{
    /// <summary>
    /// 只收没有 <see cref="IntegrationServiceAttribute"/> 的应用服务。
    /// </summary>
    ApplicationServices = 1,

    /// <summary>
    /// 只收带 <see cref="IntegrationServiceAttribute"/> 的集成服务。
    /// </summary>
    IntegrationServices = 2,

    /// <summary>
    /// 全部应用服务。
    /// </summary>
    All = ApplicationServices | IntegrationServices
}