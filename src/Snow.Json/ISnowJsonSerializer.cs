namespace Snow.Json;

/// <summary>
/// JSON 序列化门面: 全框架统一走这里, 不各自 new 序列化配置
/// <para>具体用 System.Text.Json 还是 Newtonsoft.Json 由宿主装哪个实现包决定, 调用方不感知。</para>
/// </summary>
public interface ISnowJsonSerializer
{
    /// <summary>
    /// 序列化成字符串
    /// </summary>
    /// <param name="value">待序列化的值</param>
    /// <param name="type">按指定类型序列化, 为空时取运行时类型</param>
    /// <returns>JSON 字符串</returns>
    string Serialize(object? value, Type? type = null);

    /// <summary>
    /// 序列化成 UTF-8 字节
    /// </summary>
    /// <param name="value">待序列化的值</param>
    /// <param name="type">按指定类型序列化, 为空时取运行时类型</param>
    /// <returns>UTF-8 字节</returns>
    byte[] SerializeToUtf8Bytes(object? value, Type? type = null);

    /// <summary>
    /// 反序列化成指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="json">JSON 字符串</param>
    /// <returns>反序列化结果</returns>
    T? Deserialize<T>(string json);

    /// <summary>
    /// 反序列化成指定类型
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <param name="type">目标类型</param>
    /// <returns>反序列化结果</returns>
    object? Deserialize(string json, Type type);

    /// <summary>
    /// 从流反序列化成指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="stream">JSON 流</param>
    /// <returns>反序列化结果</returns>
    T? Deserialize<T>(Stream stream);
}
