using Snow;
using Snow.Application.Dtos;

namespace System.Linq;

/// <summary>
/// <see cref="IQueryable{T}"/> 的分页扩展(对应 ABP 的 AbpPagingQueryableExtensions)。
/// <para>与 ABP 的差异: Snow 的分页请求用 <see cref="IPagedResultRequest.PageIndex"/> / <see cref="IPagedResultRequest.PageSize"/>
/// (页码从 1 开始, 对齐 Java PageRequest), 不是 ABP 的 SkipCount / MaxResultCount, 这里负责换算成 Skip/Take。</para>
/// </summary>
public static class SnowPagingQueryableExtensions
{
    /// <summary>
    /// 按分页请求裁剪查询: 等价于 Skip((PageIndex - 1) * PageSize).Take(PageSize)。
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="query">要分页的查询</param>
    /// <param name="pagedResultRequest">分页请求(实现 <see cref="IPagedResultRequest"/>)</param>
    /// <returns>裁剪后的查询(仍可继续组合 Where/OrderBy, 不触发执行)</returns>
    public static IQueryable<T> PageBy<T>(this IQueryable<T> query, IPagedResultRequest pagedResultRequest)
    {
        Check.NotNull(query, nameof(query));
        Check.NotNull(pagedResultRequest, nameof(pagedResultRequest));

        // 页码容错: 小于 1 按第一页算; 每页大小为负按 0 算
        int pageIndex = pagedResultRequest.PageIndex < 1 ? 1 : pagedResultRequest.PageIndex;
        int pageSize = pagedResultRequest.PageSize < 0 ? 0 : pagedResultRequest.PageSize;
        int skipCount = (int)Math.Min((long)(pageIndex - 1) * pageSize, int.MaxValue);

        return query.PageBy(skipCount, pageSize);
    }
}
