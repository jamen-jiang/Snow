namespace Rongia.Shared.Response;

/// <summary>
/// 分页响应(对应 Java com.rongia.shared.response.PageResponse)
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PageResponse<T>
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="totalCount">数据总数</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="pageIndex">当前页码</param>
    /// <param name="list">列表</param>
    public PageResponse(int totalCount, int pageSize, int pageIndex, IReadOnlyList<T> list)
    {
        TotalCount = totalCount;
        PageSize = pageSize;
        PageIndex = pageIndex;
        List = list;
    }

    /// <summary>
    /// 数据总数
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// 列表
    /// </summary>
    public IReadOnlyList<T> List { get; }
}
