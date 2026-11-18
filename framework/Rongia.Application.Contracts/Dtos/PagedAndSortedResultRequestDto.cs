namespace Rongia.Application.Dtos;

/// <summary>
/// 分页 + 排序请求(对应 ABP PagedAndSortedResultRequestDto)
/// </summary>
public class PagedAndSortedResultRequestDto : PagedResultRequestDto, IPagedAndSortedResultRequest
{
    /// <summary>
    /// 排序表达式,形如 "CreationTime desc, Id asc";可空表示不排序
    /// </summary>
    public string? Sorting { get; set; }
}
