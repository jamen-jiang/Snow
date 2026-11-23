using ClosedXML.Excel;

namespace Snow.Excel.ClosedXML;

/// <summary>
/// 暴露底层 ClosedXML 工作簿的服务
/// <para>给需要精细控制(合并单元格、条件格式、图表、多工作表、读公式结果)的场景用;
/// 只做一次导出/导入直接用 <see cref="IExcelService"/> 即可。</para>
/// <para>工作簿是 <see cref="IDisposable"/>, <b>调用方负责释放</b>。</para>
/// </summary>
public interface IClosedXmlExcelService : IExcelService
{
    /// <summary>
    /// 建一个空工作簿(尚未加工作表)
    /// </summary>
    /// <returns>工作簿, 调用方负责释放与保存</returns>
    XLWorkbook CreateWorkbook();

    /// <summary>
    /// 打开流里的工作簿
    /// </summary>
    /// <param name="stream">xlsx 内容流(不会被释放)</param>
    /// <returns>工作簿, 调用方负责释放</returns>
    XLWorkbook OpenWorkbook(Stream stream);

    /// <summary>
    /// 打开字节里的工作簿
    /// </summary>
    /// <param name="data">xlsx 文件内容</param>
    /// <returns>工作簿, 调用方负责释放</returns>
    XLWorkbook OpenWorkbook(byte[] data);
}
