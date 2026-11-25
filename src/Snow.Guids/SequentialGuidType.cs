namespace Snow.Guids;

/// <summary>
/// 顺序 Guid 的类型: 说明顺序部分放在哪里, 以配合不同数据库的存储格式
/// </summary>
public enum SequentialGuidType
{
    /// <summary>
    /// 按 <see cref="Guid.ToString()"/> 格式化成字符串后仍然有序; MySql 与 PostgreSql 用这种
    /// </summary>
    SequentialAsString,

    /// <summary>
    /// 按 <see cref="Guid.ToByteArray()"/> 转成字节数组后仍然有序; Oracle 用这种
    /// </summary>
    SequentialAsBinary,

    /// <summary>
    /// 有序部分放在 Data4 块的末尾; SqlServer 用这种
    /// </summary>
    SequentialAtEnd
}
