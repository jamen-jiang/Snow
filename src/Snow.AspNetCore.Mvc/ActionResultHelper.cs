using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Snow.Reflection;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 判断动作的返回类型会不会被包成对象结果(对应 ABP 的 <c>ActionResultHelper</c>)
/// <para>给 <see cref="Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptorExtensions.HasObjectResult"/> 用: 先剥掉 <c>Task</c> 外壳再判断。</para>
/// </summary>
public static class SnowActionResultHelper
{
    /// <summary>
    /// 会被当作对象结果处理的返回类型: 这三个的子类都算
    /// </summary>
    public static List<Type> ObjectResultTypes { get; } = [typeof(JsonResult), typeof(ObjectResult), typeof(NoContentResult)];

    /// <summary>
    /// 判断返回类型是不是对象结果
    /// </summary>
    /// <param name="returnType">动作的返回类型</param>
    /// <param name="excludeTypes">显式排除的类型</param>
    /// <returns>是对象结果返回 <see langword="true"/></returns>
    public static bool IsObjectResult(Type returnType, params Type[] excludeTypes)
    {
        returnType = TypeHelper.UnwrapTask(returnType);

        if (excludeTypes.Any(t => t == returnType))
        {
            return false;
        }

        // 不是 IActionResult: 返回值由输出格式化器写, 也就是对象结果
        if (!typeof(IActionResult).IsAssignableFrom(returnType))
        {
            return true;
        }

        return ObjectResultTypes.Any(t => t.IsAssignableFrom(returnType));
    }
}
