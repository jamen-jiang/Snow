using System;
using System.Collections.Generic;
using Snow.AspNetCore.Mvc.Conventions;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 与远程服务的选项(对应 ABP 的 <c>AbpAspNetCoreMvcOptions</c>)
/// </summary>
public class SnowAspNetCoreMvcOptions
{
    /// <summary>
    /// 是否压缩自动生成的脚本
    /// </summary>
    public bool? MinifyGeneratedScript { get; set; }

    /// <summary>
    /// 约定控制器选项(对应 ABP 的 AbpAspNetCoreMvcOptions.ConventionalControllers)
    /// </summary>
    public SnowConventionalControllerOptions ConventionalControllers { get; }

    /// <summary>
    /// 被模型排除时要忽略的控制器类型
    /// </summary>
    public HashSet<Type> IgnoredControllersOnModelExclusion { get; }

    /// <summary>
    /// 要从模型里移除的控制器类型
    /// </summary>
    public HashSet<Type> ControllersToRemove { get; }

    /// <summary>
    /// 是否暴露集成服务, 默认 false
    /// </summary>
    public bool ExposeIntegrationServices { get; set; } = false;

    /// <summary>
    /// 是否暴露客户端代理服务, 默认 false
    /// </summary>
    public bool ExposeClientProxyServices { get; set; } = false;

    /// <summary>
    /// 是否自动执行模型校验, 默认 true(对应 ABP 的 AutoModelValidation)
    /// </summary>
    public bool AutoModelValidation { get; set; }

    /// <summary>
    /// 是否修改控制器的 API Explorer 分组名, 默认 true
    /// </summary>
    public bool ChangeControllerModelApiExplorerGroupName { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowAspNetCoreMvcOptions()
    {
        ConventionalControllers = new SnowConventionalControllerOptions();
        IgnoredControllersOnModelExclusion = new HashSet<Type>();
        ControllersToRemove = new HashSet<Type>();
        AutoModelValidation = true;
        ChangeControllerModelApiExplorerGroupName = true;
    }
}
