namespace Snow.Clients;

/// <summary>
/// 当前客户端
/// </summary>
public interface ICurrentClient
{
    /// <summary>
    /// 是否已识别出客户端
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 客户端标识
    /// </summary>
    string? Id { get; }
}
