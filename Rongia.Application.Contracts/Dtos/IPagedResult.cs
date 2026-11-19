namespace Rongia.Application.Dtos;

/// <summary>
/// 分页结果契约(对应 ABP IPagedResult&lt;T&gt;)
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public interface IPagedResult<out T> : IListResult<T>
{
    /// <summary>
    /// 数据总数(过滤后的全量条数,非当前页条数)
    /// </summary>
    long TotalCount { get; }
}
