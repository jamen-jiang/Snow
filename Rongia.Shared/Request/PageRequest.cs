namespace Rongia.Shared.Request;

/// <summary>
/// 分页请求(对应 Java com.rongia.shared.request.PageRequest)
/// </summary>
public class PageRequest
{
    /// <summary>
    /// 当前页码(从 1 开始)
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; } = 10;
}
