using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// MVC 应用模型约定: 给远程服务控制器生成约定路由(对应 ABP 的 <c>IAbpServiceConvention</c>)。
/// </summary>
public interface ISnowServiceConvention
{
    /// <summary>
    /// 把约定应用到整个应用模型
    /// </summary>
    /// <param name="application">要应用约定的 MVC 应用模型</param>
    void Apply(ApplicationModel application);
}