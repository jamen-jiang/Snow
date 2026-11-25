namespace Snow.Guids;

/// <summary>
/// 顺序 Guid 生成器的选项
/// </summary>
public class SnowSequentialGuidGeneratorOptions
{
    /// <summary>
    /// 默认值: null(未指定); 使用时请走 <see cref="GetDefaultSequentialGuidType"/> 取值, 它会回落到一个默认类型
    /// </summary>
    public SequentialGuidType? DefaultSequentialGuidType { get; set; }

    /// <summary>
    /// 取 <see cref="DefaultSequentialGuidType"/> 的值; 为 null 时返回 <see cref="SequentialGuidType.SequentialAtEnd"/>
    /// </summary>
    /// <returns>实际使用的顺序 Guid 类型</returns>
    public SequentialGuidType GetDefaultSequentialGuidType()
    {
        return DefaultSequentialGuidType ?? SequentialGuidType.SequentialAtEnd;
    }
}
