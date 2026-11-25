using Snow.AspNetCore.Mvc.Conventions;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 与远程服务的选项(对应 ABP 的 <c>AbpAspNetCoreMvcOptions</c>)
/// </summary>
public class SnowAspNetCoreMvcOptions
{
    /// <summary>
    /// 约定控制器选项(对应 ABP 的 AbpAspNetCoreMvcOptions.ConventionalControllers)
    /// </summary>
    public SnowConventionalControllerOptions ConventionalControllers { get; }

    /// <summary>
    /// 是否暴露集成服务, 默认 false
    /// </summary>
    public bool ExposeIntegrationServices { get; set; } = false;

    /// <summary>
    /// 是否自动执行模型校验, 默认 true(对应 ABP 的 AutoModelValidation)
    /// </summary>
    public bool AutoModelValidation { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowAspNetCoreMvcOptions()
    {
        ConventionalControllers = new SnowConventionalControllerOptions();
        AutoModelValidation = true;
    }
}
