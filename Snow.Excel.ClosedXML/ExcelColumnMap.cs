using System.Collections.Concurrent;
using System.Reflection;
using Snow.Excel;

namespace Snow.Excel.ClosedXML;

/// <summary>
/// 一列的映射: 属性、表头文字、单元格格式
/// </summary>
/// <param name="Property">属性</param>
/// <param name="Header">表头文字</param>
/// <param name="Format">单元格格式(仅导出用), 可空</param>
internal sealed record ExcelColumn(PropertyInfo Property, string Header, string? Format);

/// <summary>
/// 列映射缓存: 按类型反射一次, 之后复用
/// <para>顺序规则: <see cref="ExcelColumnAttribute.Order"/> 小的在前, 未设 Order 的按属性声明顺序排在后面。</para>
/// </summary>
internal static class ExcelColumnMap
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<ExcelColumn>> Cache = new();

    /// <summary>
    /// 取类型的列映射(已排序, 已排除 <see cref="ExcelIgnoreAttribute"/> 与不可读属性)
    /// </summary>
    /// <param name="type">行类型</param>
    /// <returns>列映射</returns>
    public static IReadOnlyList<ExcelColumn> For(Type type)
    {
        return Cache.GetOrAdd(type, Build);
    }

    /// <summary>
    /// 反射构建列映射
    /// </summary>
    /// <param name="type">行类型</param>
    /// <returns>列映射</returns>
    private static IReadOnlyList<ExcelColumn> Build(Type type)
    {
        List<(ExcelColumn Column, int Order, int Index)> items = [];

        int index = 0;
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // 只认可读的实例属性: 索引器与只写属性没法导出
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (property.GetCustomAttribute<ExcelIgnoreAttribute>() is not null)
            {
                index++;
                continue;
            }

            ExcelColumnAttribute? attribute = property.GetCustomAttribute<ExcelColumnAttribute>();
            string header = string.IsNullOrWhiteSpace(attribute?.Name) ? property.Name : attribute!.Name!;

            items.Add((new ExcelColumn(property, header, attribute?.Format), attribute?.Order ?? int.MaxValue, index));
            index++;
        }

        return items
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Index)
            .Select(item => item.Column)
            .ToList();
    }
}
