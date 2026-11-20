namespace Snow.BackgroundJobs;

/// <summary>
/// 任务参数序列化: 记录落库前把参数转成字符串, 执行前再还原
/// </summary>
public interface IBackgroundJobSerializer
{
    /// <summary>
    /// 序列化
    /// </summary>
    /// <param name="value">参数对象</param>
    /// <param name="type">参数的声明类型</param>
    /// <returns>序列化后的字符串</returns>
    string Serialize(object? value, Type type);

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="value">序列化后的字符串</param>
    /// <param name="type">目标类型</param>
    /// <returns>参数对象</returns>
    object? Deserialize(string value, Type type);
}
