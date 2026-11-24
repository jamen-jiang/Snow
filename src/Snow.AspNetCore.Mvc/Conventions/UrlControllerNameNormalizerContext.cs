namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 自定义 URL 中控制器名称时的上下文(对应 ABP 同名类型)。
/// </summary>
public class UrlControllerNameNormalizerContext
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
    /// 构造器
    /// </summary>
    public UrlControllerNameNormalizerContext(string rootPath, string controllerName)
    {
        RootPath = rootPath;
        ControllerName = controllerName;
    }
}