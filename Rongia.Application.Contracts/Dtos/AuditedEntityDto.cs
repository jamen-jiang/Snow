namespace Rongia.Application.Dtos;

/// <summary>
/// 含创建/更新审计信息的实体 DTO(对应 ABP AuditedEntityDto,字段对齐领域基类 BaseUpdateEntity)
/// </summary>
public class AuditedEntityDto : CreationAuditedEntityDto
{
    /// <summary>
    /// 更新人Id(从未更新过为 null)
    /// </summary>
    public long? LastModifierId { get; set; }

    /// <summary>
    /// 更新人名称(可空)
    /// </summary>
    public string? LastModifier { get; set; }

    /// <summary>
    /// 更新时间(可空)
    /// </summary>
    public DateTime? LastModificationTime { get; set; }
}
