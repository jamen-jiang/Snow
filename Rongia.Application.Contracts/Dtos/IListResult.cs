namespace Rongia.Application.Dtos;

/// <summary>
/// 列表结果契约(对应 ABP IListResult&lt;T&gt;)
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public interface IListResult<out T>
{
    /// <summary>
    /// 列表
    /// </summary>
    IReadOnlyList<T> Items { get; }
}
