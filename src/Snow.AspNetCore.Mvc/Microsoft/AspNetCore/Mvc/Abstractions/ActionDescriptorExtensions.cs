using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Snow.AspNetCore.Mvc;
using Snow.Reflection;

namespace Microsoft.AspNetCore.Mvc.Abstractions;

/// <summary>
/// 动作描述符的便利扩展(与 ABP 的 <c>ActionDescriptorExtensions</c> 同名同位置, 七个方法逐一对应)
/// <para>位置与 ABP 一致: 在 <c>Snow.AspNetCore.Mvc</c> 的 <c>Microsoft/AspNetCore/Mvc/Abstractions/</c> 下,
/// 命名空间 <c>Microsoft.AspNetCore.Mvc.Abstractions</c> —— 用到 <see cref="ActionDescriptor"/> 的文件天然就有这个 using,
/// 类名也不带 <c>Snow</c> 前缀, 所以从 ABP 搬过来的代码一行都不用改。</para>
/// </summary>
public static class ActionDescriptorExtensions
{
    /// <summary>
    /// 当作控制器动作描述符返回, 不是则抛异常
    /// </summary>
    /// <param name="actionDescriptor">动作描述符</param>
    /// <returns>控制器动作描述符</returns>
    public static ControllerActionDescriptor AsControllerActionDescriptor(this ActionDescriptor actionDescriptor)
    {
        if (!actionDescriptor.IsControllerAction())
        {
            throw new InvalidOperationException(
                nameof(actionDescriptor) + " 应当是 " + typeof(ControllerActionDescriptor).AssemblyQualifiedName + " 类型");
        }

        return (ControllerActionDescriptor)actionDescriptor;
    }

    /// <summary>
    /// 取动作方法
    /// </summary>
    /// <param name="actionDescriptor">动作描述符</param>
    /// <returns>方法信息</returns>
    public static MethodInfo GetMethodInfo(this ActionDescriptor actionDescriptor)
    {
        return actionDescriptor.AsControllerActionDescriptor().MethodInfo;
    }

    /// <summary>
    /// 取动作的返回类型(未剥 <see cref="System.Threading.Tasks.Task"/> 外壳)
    /// </summary>
    /// <param name="actionDescriptor">动作描述符</param>
    /// <returns>返回类型</returns>
    public static Type GetReturnType(this ActionDescriptor actionDescriptor)
    {
        return actionDescriptor.GetMethodInfo().ReturnType;
    }

    /// <summary>
    /// 该动作的返回类型会不会被包成对象结果
    /// </summary>
    /// <param name="actionDescriptor">动作描述符</param>
    /// <returns>是对象结果返回 <see langword="true"/></returns>
    public static bool HasObjectResult(this ActionDescriptor actionDescriptor)
    {
        return ActionResultHelper.IsObjectResult(actionDescriptor.GetReturnType());
    }

    /// <summary>
    /// 是不是控制器动作
    /// </summary>
    /// <param name="actionDescriptor">动作描述符</param>
    /// <returns>是返回 <see langword="true"/></returns>
    public static bool IsControllerAction(this ActionDescriptor actionDescriptor)
    {
        return actionDescriptor is ControllerActionDescriptor;
    }


}
