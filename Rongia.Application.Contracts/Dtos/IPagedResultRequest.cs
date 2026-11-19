namespace Rongia.Application.Dtos;

/// <summary>
/// 分页请求契约,页码从 1 开始(对齐 Java PageRequest,而非 ABP 的 SkipCount/MaxResultCount)
/// </summary>
public interface IPagedResultRequest
{
    /// <summary>
    /// 当前页码(从 1 开始)
    /// </summary>
    int PageIndex { get; }

    /// <summary>
    /// 每页大小
    /// </summary>
    int PageSize { get; }
}
