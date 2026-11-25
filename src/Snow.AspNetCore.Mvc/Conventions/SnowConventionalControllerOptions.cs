using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 约定控制器选项。
/// </summary>
public class SnowConventionalControllerOptions
{
    /// <summary>
    /// 不做请求体绑定的类型(如表单文件)
    /// </summary>
    public List<Type> FormBodyBindingIgnoredTypes { get; }

    /// <summary>
    /// 生成 URL 时忽略的控制器名后缀
    /// </summary>
    public string[] IgnoredUrlSuffixesInControllerNames { get; set; } = new[] { "Integration" };

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowConventionalControllerOptions()
    {
        FormBodyBindingIgnoredTypes = new List<Type>
        {
            typeof(IFormFile)
        };
    }
}
