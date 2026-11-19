using System;

namespace Rongia.Security.Middleware;

/// <summary>
/// 关闭控制器上的框架特性: 目前只用于让框架中间件跳过该控制器
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DisableRongiaFeaturesAttribute : Attribute
{
    /// <summary>
    /// 是否跳过框架中间件
    /// </summary>
    public bool DisableMiddleware { get; set; }
}
