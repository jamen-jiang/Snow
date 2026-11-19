namespace Rongia.Application.Dtos;

/// <summary>
/// 实体数据传输对象契约(主键为 long)
/// </summary>
public interface IEntityDto
{
    /// <summary>
    /// 主键
    /// </summary>
    long Id { get; }
}
