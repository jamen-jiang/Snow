using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 自定义 URL 中 action 名称时的上下文(对应 ABP 同名类型)。
/// </summary>
public class UrlActionNameNormalizerContext
{
    /// <summary>
    /// 根路径
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// 控制器名
    /// </summary>
    public string ControllerName { get; }

    /// <summary>
    /// 当前 action 模型
    /// </summary>
    public ActionModel Action { get; }

    /// <summary>
    /// 按默认规则算出的 URL 中 action 名称
    /// </summary>
    public string ActionNameInUrl { get; }

    /// <summary>
    /// HTTP 动词
    /// </summary>
    public string HttpMethod { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    public UrlActionNameNormalizerContext(string rootPath, string controllerName, ActionModel action, string actionNameInUrl, string httpMethod)
    {
        RootPath = rootPath;
        ControllerName = controllerName;
        Action = action;
        ActionNameInUrl = actionNameInUrl;
        HttpMethod = httpMethod;
    }
}