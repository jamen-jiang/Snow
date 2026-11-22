namespace Snow.Excel;

/// <summary>
/// 导出选项
/// </summary>
public class ExcelExportOptions
{
    /// <summary>
    /// 工作表名(默认 "Sheet1"; Excel 不允许 : \\ / ? * [ ] 且最长 31 字符)
    /// </summary>
    public string SheetName { get; set; } = "Sheet1";

    /// <summary>
    /// 是否写表头行, 默认 true
    /// </summary>
    public bool IncludeHeaderRow { get; set; } = true;

    /// <summary>
    /// 是否冻结表头行, 默认 true(表头行不写时无效)
    /// </summary>
    public bool FreezeHeaderRow { get; set; } = true;

    /// <summary>
    /// 是否给表头加自动筛选, 默认 true(表头行不写时无效)
    /// </summary>
    public bool AutoFilter { get; set; } = true;

    /// <summary>
    /// 是否按内容自动调整列宽, 默认 true
    /// <para>行数很多(上万)时可关掉: ClosedXML 的自动列宽要逐格测文本宽度, 是导出里最慢的一步。</para>
    /// </summary>
    public bool AutoFitColumns { get; set; } = true;

    /// <summary>
    /// 日期时间的单元格格式, 默认 "yyyy-MM-dd HH:mm:ss"
    /// </summary>
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
}
