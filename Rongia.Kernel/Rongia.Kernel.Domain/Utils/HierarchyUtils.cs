using Rongia.Kernel.Domain.Entities;
using Rongia.Kernel.Domain.Exceptions;

namespace Rongia.Kernel.Domain.Utils;

/// <summary>
/// 树形层级编排工具:只做纯计算,数据由调用方查好后传入(对应 Java HierarchyUtils)
/// </summary>
public static class HierarchyUtils
{
    /// <summary>
    /// 层级路径的分隔符
    /// </summary>
    private const string Separator = "-";

    /// <summary>
    /// 根据父节点计算层级路径。
    /// <para>路径只存祖先、不含自己:父是 5、祖父是 3、曾祖是 1,这一条就是 "1-3-5",直接挂在根下面的那一条是 "1"。
    /// 查整棵子树时把列前后各补一个分隔符再按 "-5-" 匹配,所以这个分隔符和仓储里的条件查询是一体的,改一处必须改另一处。</para>
    /// </summary>
    /// <param name="parentId">父节点Id(可空)</param>
    /// <param name="parent">父节点实体,由调用方查询后传入(可空)</param>
    /// <param name="label">父节点名称,用于异常信息</param>
    /// <returns>层级路径(如 "1-3-5"),无父节点返回 null</returns>
    /// <exception cref="DomainException">父节点Id非空但实体不存在时抛出</exception>
    public static string? ResolveParentPath<T>(long? parentId, T? parent, string label) where T : IParent
    {
        if (parentId is null)
        {
            return null;
        }
        if (parent is null)
        {
            throw new DomainException(label + "不存在: " + parentId);
        }
        return BuildParentPath(parent);
    }

    /// <summary>
    /// 给一批子节点重设层级路径
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="children">子节点列表,由调用方查询后传入</param>
    public static void ResetChildrenParentPath<T>(T parent, IReadOnlyList<T> children) where T : IParent
    {
        if (children.Count == 0)
        {
            return;
        }
        var parentPath = BuildParentPath(parent);
        foreach (var child in children)
        {
            child.ChangeParentPath(parentPath);
        }
    }

    /// <summary>
    /// 拼出一个节点下面直接子节点该有的路径:父节点自己的路径(可空)后面接父Id
    /// </summary>
    private static string BuildParentPath<T>(T parent) where T : IParent
    {
        return string.IsNullOrWhiteSpace(parent.ParentPath)
            ? parent.Id.ToString()
            : parent.ParentPath + Separator + parent.Id;
    }
}
