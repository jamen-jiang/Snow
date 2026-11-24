namespace Snow.Auditing;

/// <summary>
/// 创建审计契约: 记录谁在什么时候创建
/// </summary>
public interface ICreationAudited : IHasCreationTime
{
    /// <summary>
    /// 创建人Id
    /// </summary>
    long CreatorId { get; set; }

    /// <summary>
    /// 创建人名称
    /// </summary>
    string Creator { get; set; }
}
