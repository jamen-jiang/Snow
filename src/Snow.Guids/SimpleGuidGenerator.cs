namespace Snow.Guids;

/// <summary>
/// <see cref="IGuidGenerator"/> 的实现: 直接用 <see cref="Guid.NewGuid"/>
/// </summary>
public class SimpleGuidGenerator : IGuidGenerator
{
    /// <summary>
    /// 单例: 没有登记生成器时作为兜底使用
    /// </summary>
    public static SimpleGuidGenerator Instance { get; } = new SimpleGuidGenerator();

    /// <summary>
    /// 创建一个新的 <see cref="Guid"/>
    /// </summary>
    /// <returns>新的 Guid</returns>
    public virtual Guid Create()
    {
        return Guid.NewGuid();
    }
}
