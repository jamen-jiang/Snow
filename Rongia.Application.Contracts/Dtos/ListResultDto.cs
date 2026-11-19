namespace Rongia.Application.Dtos;

/// <summary>
/// 列表结果(对应 ABP ListResultDto&lt;T&gt;)
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public class ListResultDto<T> : IListResult<T>
{
    /// <summary>
    /// 无参构造器(供序列化使用)
    /// </summary>
    public ListResultDto()
    {
        Items = Array.Empty<T>();
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="items">列表</param>
    public ListResultDto(IReadOnlyList<T> items)
    {
        Items = items;
    }

    /// <summary>
    /// 列表
    /// </summary>
    public IReadOnlyList<T> Items { get; set; }
}
