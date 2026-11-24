namespace Snow.Auditing;

/// <summary>
/// 修改审计契约: 记录谁在什么时候最后修改
/// </summary>
public interface IModificationAudited : IHasModificationTime
{
    /// <summary>
    /// 最后修改人Id
    /// </summary>
    long? LastModifierId { get; set; }

    /// <summary>
    /// 最后修改人名称
    /// </summary>
    string? LastModifier { get; set; }
}
