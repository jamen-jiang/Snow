using System;
using System.Reflection;

namespace Snow.BackgroundJobs;

/// <summary>
/// 类型名与 <see cref="Type"/> 的互转: 任务记录里只存字符串, 执行时再还原
/// <para>存的是"全名 + 程序集简单名", 不带版本号 —— 升级依赖后老记录照样能还原。</para>
/// </summary>
public static class BackgroundJobTypeName
{
    /// <summary>
    /// 取类型的持久化名字
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>形如 <c>Ns.Type, Assembly</c> 的字符串</returns>
    public static string GetName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }

    /// <summary>
    /// 还原类型
    /// </summary>
    /// <param name="typeName">持久化的类型名</param>
    /// <returns>类型, 找不到为 null</returns>
    public static Type? Resolve(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        Type? type = Type.GetType(typeName, throwOnError: false);

        if (type is not null)
        {
            return type;
        }

        // 程序集名对不上(改过程序集名、或类型在别的程序集里)时, 退化成按全名在已加载程序集里找
        string fullName = typeName.Split(',')[0].Trim();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(fullName, throwOnError: false);

            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }
}
