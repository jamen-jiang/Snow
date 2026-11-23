namespace Snow.Auditing;

/// <summary>
/// 审计序列化器: 把参数等对象转成可落库的文本
/// </summary>
public interface IAuditSerializer
{
    /// <summary>
    /// 序列化
    /// </summary>
    /// <param name="value">待序列化的对象</param>
    /// <returns>文本, 失败时返回说明性占位而不是抛异常</returns>
    string Serialize(object? value);
}
