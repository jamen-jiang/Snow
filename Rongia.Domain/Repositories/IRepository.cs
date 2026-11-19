using Rongia.Domain.Entities;

namespace Rongia.Domain.Repositories;

/// <summary>
/// 仓储基础接口(对应 Java Repository)
/// </summary>
public interface IRepository<T>
    where T : IAggregateRoot
{
    /// <summary>
    /// 按主键查询,不存在返回 null
    /// </summary>
    T? GetAsync(long id);

    /// <summary>
    /// <para>根据多个主键获取实体列表</para>
    /// </summary>
    /// <param name="ids">实体主键集合</param>
    /// <returns>实体列表</returns>
    Task<List<T>> Get(IEnumerable<long> ids);

    /// <summary>
    /// <para>获取除指定主键外的所有实体</para>
    /// </summary>
    /// <param name="id">要排除的实体主键</param>
    /// <returns>排除指定实体后的列表</returns>
    Task<List<T>> GetNoById(long id);

    /// <summary>
    /// <para>获取除指定多个主键外的所有实体</para>
    /// </summary>
    /// <param name="ids">要排除的实体主键集合</param>
    /// <returns>排除指定实体后的列表</returns>
    Task<List<T>> GetNoByIds(IEnumerable<long> ids);
}
