namespace Snow.Auditing;

/// <summary>
/// 实体审计字段填充器: 按实体实现的审计契约自动写创建/修改/删除的人与时间
/// </summary>
public interface IAuditPropertySetter
{
    /// <summary>
    /// 填创建字段(创建时间、创建人)
    /// </summary>
    /// <param name="entity">实体</param>
    void SetCreationProperties(object entity);

    /// <summary>
    /// 填修改字段(最后修改时间、最后修改人)
    /// </summary>
    /// <param name="entity">实体</param>
    void SetModificationProperties(object entity);

    /// <summary>
    /// 填删除字段(软删除标记、删除时间、删除人)
    /// </summary>
    /// <param name="entity">实体</param>
    void SetDeletionProperties(object entity);
}
