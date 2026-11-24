using System;

namespace Snow.AspNetCore.Controllers;

/// <summary>
/// 标记当前控制器替换掉指定的控制器类型(对应 ABP 的 <c>ReplaceControllersAttribute</c>)。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ReplaceControllersAttribute : Attribute
{
    /// <summary>
    /// 被替换的控制器类型
    /// </summary>
    public Type[] ControllerTypes { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="controllerTypes">被替换的控制器类型</param>
    public ReplaceControllersAttribute(params Type[] controllerTypes)
    {
        ControllerTypes = controllerTypes ?? Type.EmptyTypes;
    }
}