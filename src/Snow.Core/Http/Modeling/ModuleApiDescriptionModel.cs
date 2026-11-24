namespace Snow.Http.Modeling;

/// <summary>
/// 模块 API 描述模型的常量(对应 ABP 的 <c>ModuleApiDescriptionModel</c>, 这里只保留约定路由用到的默认值)。
/// </summary>
public static class ModuleApiDescriptionModel
{
    /// <summary>
    /// 默认根路径
    /// </summary>
    public const string DefaultRootPath = "app";

    /// <summary>
    /// 默认远程服务名
    /// </summary>
    public const string DefaultRemoteServiceName = "Default";
}