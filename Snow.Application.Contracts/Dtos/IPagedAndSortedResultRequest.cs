namespace Snow.Application.Dtos;

/// <summary>
/// 分页 + 排序请求契约(对应 ABP IPagedAndSortedResultRequest)
/// </summary>
public interface IPagedAndSortedResultRequest : IPagedResultRequest
{
    /// <summary>
    /// 排序表达式,形如 "CreationTime desc, Id asc";可空表示不排序
    /// </summary>
    string? Sorting { get; }
}
