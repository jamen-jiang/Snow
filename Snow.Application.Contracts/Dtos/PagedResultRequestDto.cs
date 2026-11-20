namespace Snow.Application.Dtos;

/// <summary>
/// 分页请求(对应 ABP PagedResultRequestDto,但采用页码语义以对齐 Java PageRequest)
/// </summary>
public class PagedResultRequestDto : IPagedResultRequest
{
    /// <summary>
    /// 当前页码(从 1 开始),默认 1
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页大小,默认 10
    /// </summary>
    public int PageSize { get; set; } = 10;
}
