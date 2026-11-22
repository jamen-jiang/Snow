namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 与自动 API 的选项(对应 ABP 的 AbpAspNetCoreMvcOptions)
/// </summary>
public sealed class SnowMvcOptions
{
    /// <summary>
    /// 自动 API 的根路径, 默认 "api/app"(对应 ABP 的 ModuleApiDescriptionModel.DefaultRootPath)
    /// </summary>
    public string RootPath { get; set; } = "api/app";

    /// <summary>
    /// 是否把应用服务自动暴露成 Controller, 关闭后只保留手写 Controller
    /// </summary>
    public bool AutoApiControllers { get; set; } = true;

    /// <summary>
    /// 是否默认把响应包装成 <see cref="SnowApiResponse"/>, 单个方法可用 <see cref="SnowWrapResultAttribute"/> 覆盖
    /// </summary>
    public bool WrapResultByDefault { get; set; } = true;

    /// <summary>
    /// 是否把异常消息原样返回给客户端, 默认 false 只回"服务器内部错误"
    /// </summary>
    public bool IncludeExceptionMessage { get; set; } = false;

    /// <summary>
    /// 是否把模型校验错误自动回 400, 默认 true
    /// </summary>
    public bool AutoModelStateValidation { get; set; } = true;
}
