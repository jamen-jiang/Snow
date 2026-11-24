using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 按 ABP 规则计算约定路由模板(对应 ABP 的 <c>IConventionalRouteBuilder</c>)。
/// </summary>
public interface IConventionalRouteBuilder
{
    /// <summary>
    /// 计算一个 action 的路由模板
    /// </summary>
    /// <param name="rootPath">API 根路径</param>
    /// <param name="controllerName">控制器名称</param>
    /// <param name="action">动作模型</param>
    /// <param name="httpMethod">HTTP 方法</param>
    /// <param name="configuration">当前控制器的约定配置</param>
    /// <returns>路由模板</returns>
    string Build(string rootPath, string controllerName, ActionModel action, string httpMethod, ConventionalControllerSetting? configuration);
}