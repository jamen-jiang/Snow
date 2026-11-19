namespace Rongia.Shared.Request;

/// <summary>
/// 带唯一值列表的分页请求(对应 Java com.rongia.shared.request.PageRequestWithUnique)
/// </summary>
/// <typeparam name="T">唯一值类型</typeparam>
public class PageRequestWithUnique<T> : PageRequest
{
    /// <summary>
    /// 唯一值列表(可空)
    /// </summary>
    public IReadOnlyList<T>? UniqueValues { get; set; }
}
