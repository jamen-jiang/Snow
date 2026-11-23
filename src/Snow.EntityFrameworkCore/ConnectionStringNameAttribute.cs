using System.Reflection;

namespace Snow.EntityFrameworkCore;

/// <summary>
/// 指定上下文使用的连接串名称(对齐 ABP 的 <c>ConnectionStringNameAttribute</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ConnectionStringNameAttribute : Attribute
{
    /// <summary>
    /// 连接串名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">连接串名称</param>
    public ConnectionStringNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
    }

    /// <summary>
    /// 取上下文类型对应的连接串名称
    /// </summary>
    /// <typeparam name="TDbContext">上下文类型</typeparam>
    /// <returns>连接串名称</returns>
    public static string GetConnStringName<TDbContext>()
    {
        return GetConnStringName(typeof(TDbContext));
    }

    /// <summary>
    /// 取上下文类型对应的连接串名称: 有特性用特性, 否则去掉类型名末尾的 DbContext
    /// </summary>
    /// <param name="type">上下文类型</param>
    /// <returns>连接串名称</returns>
    public static string GetConnStringName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        ConnectionStringNameAttribute? attribute = type.GetCustomAttribute<ConnectionStringNameAttribute>(inherit: true);
        if (attribute is not null)
        {
            return attribute.Name;
        }

        const string suffix = "DbContext";
        string typeName = type.Name;

        return typeName.EndsWith(suffix, StringComparison.Ordinal)
            ? typeName[..^suffix.Length]
            : typeName;
    }
}
