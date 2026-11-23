using System;
using System.Collections.Generic;

namespace Snow.Messaging.Internal;

/// <summary>
/// 一个对象的"位置信息"快照:程序集 / 命名空间 / 类型名,用于给处理器排序和去重
/// </summary>
internal class ObjectDetails : IComparer<ObjectDetails>
{
    /// <summary>
    /// 类型名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 所在程序集名
    /// </summary>
    public string? AssemblyName { get; }

    /// <summary>
    /// 相对定位串(命名空间去掉程序集前缀后的部分)
    /// </summary>
    public string? Location { get; }

    /// <summary>
    /// 对应的原始对象(处理器实例等)
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// 对象运行时类型
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// 是否已被更具体的派生类型覆盖(覆盖者保留,被覆盖者剔除)
    /// </summary>
    public bool IsOverridden { get; set; }

    /// <summary>
    /// 构造器:从对象实例提取上述信息
    /// </summary>
    /// <param name="value">要分析的对象</param>
    public ObjectDetails(object value)
    {
        Value = value;
        Type = Value.GetType();
        var exceptionHandlerType = value.GetType();

        Name = exceptionHandlerType.Name;
        AssemblyName = exceptionHandlerType.Assembly.GetName().Name;
        Location = exceptionHandlerType.Namespace?.Replace($"{AssemblyName}.", string.Empty);
    }

    /// <summary>
    /// 两级比较:先比程序集,再比命名空间,最后比定位串深度
    /// </summary>
    /// <param name="x">左对象</param>
    /// <param name="y">右对象</param>
    /// <returns>负数表示 x 优先,正数表示 y 优先,0 表示同级</returns>
    public int Compare(ObjectDetails? x, ObjectDetails? y)
    {
        if (x == null)
        {
            return 1;
        }

        if (y == null)
        {
            return -1;
        }

        return CompareByAssembly(x, y) ?? CompareByNamespace(x, y) ?? CompareByLocation(x, y);
    }

    /// <summary>
    /// 按"是否属于当前程序集"比较
    /// </summary>
    /// <param name="x">左对象</param>
    /// <param name="y">右对象</param>
    /// <returns>
    /// 属于当前程序集者优先;两者都不属于当前程序集时视为同级;
    /// 两者都属于当前程序集时本规则无法区分,返回 null 交由下一级规则
    /// </returns>
    private int? CompareByAssembly(ObjectDetails x, ObjectDetails y)
    {
        if (x.AssemblyName == AssemblyName && y.AssemblyName != AssemblyName)
        {
            return -1;
        }

        if (x.AssemblyName != AssemblyName && y.AssemblyName == AssemblyName)
        {
            return 1;
        }
        if (x.AssemblyName != AssemblyName && y.AssemblyName != AssemblyName)
        {
            return 0;
        }

        return null;
    }

    /// <summary>
    /// 按"是否位于当前命名空间(含子命名空间)"比较
    /// </summary>
    /// <param name="x">左对象</param>
    /// <param name="y">右对象</param>
    /// <returns>
    /// 位于当前/子命名空间者优先;两者都在则视为同级;
    /// 都不在时本规则无法区分,返回 null 交由下一级规则
    /// </returns>
    private int? CompareByNamespace(ObjectDetails x, ObjectDetails y)
    {
        if (Location is null || x.Location is null || y.Location is null)
        {
            return 0;
        }

        if (x.Location.StartsWith(Location, StringComparison.Ordinal) && !y.Location.StartsWith(Location, StringComparison.Ordinal))
        {
            return -1;
        }

        if (!x.Location.StartsWith(Location, StringComparison.Ordinal) && y.Location.StartsWith(Location, StringComparison.Ordinal))
        {
            return 1;
        }
        if (x.Location.StartsWith(Location, StringComparison.Ordinal) && y.Location.StartsWith(Location, StringComparison.Ordinal))
        {
            return 0;
        }

        return null;
    }

    /// <summary>
    /// 按"在当前定位串中的深度"比较
    /// </summary>
    /// <param name="x">左对象</param>
    /// <param name="y">右对象</param>
    /// <returns>属于当前定位串者优先;都在其中时越近(定位串越长)越优先;都不在则视为同级</returns>
    private int CompareByLocation(ObjectDetails x, ObjectDetails y)
    {
        if (Location is null || x.Location is null || y.Location is null)
        {
            return 0;
        }

        if (Location.StartsWith(x.Location, StringComparison.Ordinal) && !Location.StartsWith(y.Location, StringComparison.Ordinal))
        {
            return -1;
        }

        if (!Location.StartsWith(x.Location, StringComparison.Ordinal) && Location.StartsWith(y.Location, StringComparison.Ordinal))
        {
            return 1;
        }
        if (x.Location.Length > y.Location.Length)
        {
            return -1;
        }
        if (x.Location.Length < y.Location.Length)
        {
            return 1;
        }
        return 0;
    }
}
