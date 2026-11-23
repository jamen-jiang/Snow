namespace Snow.Excel;

/// <summary>
/// 列映射: 指定属性对应的表头文字、导出顺序与单元格格式
/// <para>不带本注解时表头用属性名, 顺序按属性声明顺序; 导入时同样按表头文字匹配。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ExcelColumnAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">表头文字, 空则用属性名</param>
    public ExcelColumnAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// 表头文字(导入时按它匹配列)
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// 导出顺序, 小的在前; 不设则按属性声明顺序排在最后
    /// </summary>
    public int Order { get; set; } = int.MaxValue;

    /// <summary>
    /// 单元格格式(仅导出用), 如 "yyyy-mm-dd"、"#,##0.00"; 日期时间不设时用 <see cref="ExcelExportOptions.DateTimeFormat"/>
    /// </summary>
    public string? Format { get; set; }
}

/// <summary>
/// 排除属性: 既不导出成列, 也不参与导入
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ExcelIgnoreAttribute : Attribute;
