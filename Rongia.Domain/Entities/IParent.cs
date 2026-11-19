namespace Rongia.Domain.Entities;

/// <summary>
/// 树形层级实体:拥有父节点与层级路径(对应 Java Parent)。
/// <para>列映射(ParentId / ParentPath)由各实现类自己的字段特性决定 —— 与 Java 相同,
/// 接口不写映射特性。</para>
/// </summary>
public interface IParent : IEntity
{
    /// <summary>
    /// 父节点Id,根节点为 null(可空)
    /// </summary>
    long? ParentId { get; }

    /// <summary>
    /// 层级路径(如 "1-3-5"),根节点为 null(可空)
    /// </summary>
    string? ParentPath { get; }

    /// <summary>
    /// 重设层级路径(由层级编排逻辑调用)
    /// </summary>
    void ChangeParentPath(string? parentPath);
}
