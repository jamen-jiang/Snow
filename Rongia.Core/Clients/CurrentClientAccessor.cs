using Rongia.DependencyInjection;

namespace Rongia.Clients;

/// <summary>
/// 当前客户端访问器默认实现: 进程内保存
/// </summary>
public class CurrentClientAccessor : ICurrentClientAccessor, ISingletonDependency
{
    /// <inheritdoc />
    public ClientInfo? Current { get; set; }
}
