namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 的选项(对应 ABP 的 <c>AbpAspNetCoreMvcOptions</c>)
/// </summary>
public class SnowAspNetCoreMvcOptions
{
    /// <summary>
    /// 是否自动执行模型校验, 默认 true(对应 ABP 的 AutoModelValidation)
    /// </summary>
    public bool AutoModelValidation { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowAspNetCoreMvcOptions()
    {
        AutoModelValidation = true;
    }
}
