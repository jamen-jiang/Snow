namespace Snow;

/// <summary>
/// 空释放器(对应 ABP 的 <c>Volo.Abp.NullDisposable</c>, 位于 Volo.Abp.Core): 什么都不做, 用于「无需释放」的分支, 避免多分配一次。
/// </summary>
public sealed class NullDisposable : IDisposable
{
    /// <summary>
    /// 共享实例
    /// </summary>
    public static NullDisposable Instance { get; } = new NullDisposable();

    private NullDisposable()
    {
    }

    /// <summary>
    /// 什么都不做
    /// </summary>
    public void Dispose()
    {
    }
}
