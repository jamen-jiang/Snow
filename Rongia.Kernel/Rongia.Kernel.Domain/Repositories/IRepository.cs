using Rongia.Kernel.Domain.Aggregates;

namespace Rongia.Kernel.Domain.Repositories;

/// <summary>
/// 仓储基础接口(对应 Java Repository)
/// </summary>
public interface IRepository<T> where T : IAggregateRoot
{
    /// <summary>
    /// 按主键查询,不存在返回 null
    /// </summary>
    T? Get(long id);

    /// <summary>
    /// 保存(新增或更新)
    /// </summary>
    void Save(T aggregateRoot);
}
