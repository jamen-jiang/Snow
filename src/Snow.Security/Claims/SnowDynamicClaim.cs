namespace Snow.Security.Claims;

/// <summary>
/// 动态声明(对应 ABP 的 <c>AbpDynamicClaim</c>): 可序列化的类型/值对
/// </summary>
[Serializable]
public class SnowDynamicClaim
{
    /// <summary>
    /// 声明类型
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 声明值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="type">声明类型</param>
    /// <param name="value">声明值</param>
    public SnowDynamicClaim(string type, string? value)
    {
        Type = type;
        Value = value;
    }
}
