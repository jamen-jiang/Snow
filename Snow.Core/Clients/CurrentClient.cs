using Snow.DependencyInjection;

namespace Snow.Clients;

/// <summary>
/// 当前客户端默认实现: 从 <see cref="ICurrentClientAccessor"/> 读取
/// </summary>
public class CurrentClient : ICurrentClient, ITransientDependency
{
    /// <inheritdoc />
    public virtual bool IsAuthenticated => Id != null;

    /// <inheritdoc />
    public virtual string? Id => _currentClientAccessor.Current?.ClientId;

    private readonly ICurrentClientAccessor _currentClientAccessor;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="currentClientAccessor">当前客户端访问器</param>
    public CurrentClient(ICurrentClientAccessor currentClientAccessor)
    {
        _currentClientAccessor = currentClientAccessor;
    }
}
