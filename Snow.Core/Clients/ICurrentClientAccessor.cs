namespace Snow.Clients;

/// <summary>
/// 当前客户端访问器: 由宿主在请求进入时写入, 释放时还原
/// </summary>
public interface ICurrentClientAccessor
{
    /// <summary>
    /// 当前客户端信息
    /// </summary>
    ClientInfo? Current { get; set; }
}
