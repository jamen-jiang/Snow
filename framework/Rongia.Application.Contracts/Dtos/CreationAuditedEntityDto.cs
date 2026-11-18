namespace Rongia.Application.Dtos;

/// <summary>
/// 含创建审计信息的实体 DTO(对应 ABP CreationAuditedEntityDto,字段对齐领域基类 BaseCreateEntity)
/// </summary>
public class CreationAuditedEntityDto : EntityDto
{
    /// <summary>
    /// 创建人Id
    /// </summary>
    public long CreatorId { get; set; }

    /// <summary>
    /// 创建人名称
    /// </summary>
    public string Creator { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }
}
