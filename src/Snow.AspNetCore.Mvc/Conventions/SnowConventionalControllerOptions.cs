using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Snow.Http.Modeling;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 约定控制器选项(对应 ABP 的 <c>AbpConventionalControllerOptions</c>): 宿主用 <see cref="Create"/> 显式登记要扫描的程序集。
/// </summary>
public class SnowConventionalControllerOptions
{
    /// <summary>
    /// 已登记的约定控制器设置
    /// </summary>
    public ConventionalControllerSettingList ConventionalControllerSettings { get; }

    /// <summary>
    /// 不做请求体绑定的类型(如表单文件)
    /// </summary>
    public List<Type> FormBodyBindingIgnoredTypes { get; }

    /// <summary>
    /// 是否使用旧版 URL 风格, 默认 false
    /// </summary>
    public bool UseV3UrlStyle { get; set; }

    /// <summary>
    /// 生成 URL 时忽略的控制器名后缀
    /// </summary>
    public string[] IgnoredUrlSuffixesInControllerNames { get; set; } = new[] { "Integration" };

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowConventionalControllerOptions()
    {
        ConventionalControllerSettings = new ConventionalControllerSettingList();
        FormBodyBindingIgnoredTypes = new List<Type>
        {
            typeof(IFormFile)
        };
    }

    /// <summary>
    /// 登记一个程序集, 并立即扫描其中的远程服务类型。
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <param name="optionsAction">选项配置回调, 可为 null</param>
    public SnowConventionalControllerOptions Create(Assembly assembly, Action<ConventionalControllerSetting>? optionsAction = null)
    {
        var setting = new ConventionalControllerSetting(
            assembly,
            ModuleApiDescriptionModel.DefaultRootPath,
            ModuleApiDescriptionModel.DefaultRemoteServiceName
        );

        optionsAction?.Invoke(setting);
        setting.Initialize();
        ConventionalControllerSettings.Add(setting);
        return this;
    }
}