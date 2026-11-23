namespace Snow.Application.Dtos;

/// <summary>
/// 分页结果(对应 ABP PagedResultDto&lt;T&gt;)
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public class PagedResultDto<T> : ListResultDto<T>, IPagedResult<T>
{
    /// <summary>
    /// 无参构造器(供序列化使用)
    /// </summary>
    public PagedResultDto()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="totalCount">数据总数</param>
    /// <param name="items">当前页列表</param>
    public PagedResultDto(long totalCount, IReadOnlyList<T> items)
        : base(items)
    {
        TotalCount = totalCount;
    }

    /// <summary>
    /// 数据总数
    /// </summary>
    public long TotalCount { get; set; }
}
