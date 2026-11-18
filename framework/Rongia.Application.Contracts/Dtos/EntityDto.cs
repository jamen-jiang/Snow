namespace Rongia.Application.Dtos;

/// <summary>
/// 仅含主键的实体 DTO(对应 ABP EntityDto)
/// </summary>
public class EntityDto : IEntityDto
{
    /// <summary>
    /// 主键,未持久化实体为 0
    /// </summary>
    public long Id { get; set; }
}
