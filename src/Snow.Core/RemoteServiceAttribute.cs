using System;
using System.Reflection;

namespace Snow;

/// <summary>
/// 标记一个接口/类/方法为远程服务, 并可单独控制是否暴露以及是否输出元数据(对应 ABP 的 <c>RemoteServiceAttribute</c>)。
/// </summary>
[Serializable]
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
public class RemoteServiceAttribute : Attribute
{
    /// <summary>
    /// 是否启用该远程服务, 默认 true。
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 是否输出元数据, 默认 true。
    /// </summary>
    public bool IsMetadataEnabled { get; set; }

    /// <summary>
    /// 远程服务的分组名; 同一模块的服务通常用同一个名字。
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="isEnabled">是否启用, 默认 true</param>
    public RemoteServiceAttribute(bool isEnabled = true)
    {
        IsEnabled = isEnabled;
        IsMetadataEnabled = true;
    }

    /// <summary>
    /// 是否对指定类型启用
    /// </summary>
    public virtual bool IsEnabledFor(Type type) => IsEnabled;

    /// <summary>
    /// 是否对指定方法启用
    /// </summary>
    public virtual bool IsEnabledFor(MethodInfo method) => IsEnabled;

    /// <summary>
    /// 是否对指定类型输出元数据
    /// </summary>
    public virtual bool IsMetadataEnabledFor(Type type) => IsMetadataEnabled;

    /// <summary>
    /// 是否对指定方法输出元数据
    /// </summary>
    public virtual bool IsMetadataEnabledFor(MethodInfo method) => IsMetadataEnabled;

    /// <summary>
    /// 类型上是否显式启用了远程服务
    /// </summary>
    public static bool IsExplicitlyEnabledFor(Type type)
    {
        var remoteServiceAttr = type.GetTypeInfo().GetSingleAttributeOrNull<RemoteServiceAttribute>();
        return remoteServiceAttr != null && remoteServiceAttr.IsEnabledFor(type);
    }

    /// <summary>
    /// 类型上是否显式禁用了远程服务
    /// </summary>
    public static bool IsExplicitlyDisabledFor(Type type)
    {
        var remoteServiceAttr = type.GetTypeInfo().GetSingleAttributeOrNull<RemoteServiceAttribute>();
        return remoteServiceAttr != null && !remoteServiceAttr.IsEnabledFor(type);
    }

    /// <summary>
    /// 类型上是否显式启用了元数据
    /// </summary>
    public static bool IsMetadataExplicitlyEnabledFor(Type type)
    {
        var remoteServiceAttr = type.GetTypeInfo().GetSingleAttributeOrNull<RemoteServiceAttribute>();
        return remoteServiceAttr != null && remoteServiceAttr.IsMetadataEnabledFor(type);
    }

    /// <summary>
    /// 类型上是否显式禁用了元数据
    /// </summary>
    public static bool IsMetadataExplicitlyDisabledFor(Type type)
    {
        var remoteServiceAttr = type.GetTypeInfo().GetSingleAttributeOrNull<RemoteServiceAttribute>();
        return remoteServiceAttr != null && !remoteServiceAttr.IsMetadataEnabledFor(type);
    }

    /// <summary>
    /// 方法上是否显式禁用了元数据
    /// </summary>
    public static bool IsMetadataExplicitlyDisabledFor(MethodInfo method)
    {
        var remoteServiceAttr = method.GetSingleAttributeOrNull<RemoteServiceAttribute>();
        return remoteServiceAttr != null && !remoteServiceAttr.IsMetadataEnabledFor(method);
    }

    /// <summary>
    /// 方法上是否显式启用了元数据
    /// </summary>
    public static bool IsMetadataExplicitlyEnabledFor(MethodInfo method)
    {
        var remoteServiceAttr = method.GetSingleAttributeOrNull<RemoteServiceAttribute>();
        return remoteServiceAttr != null && remoteServiceAttr.IsMetadataEnabledFor(method);
    }
}