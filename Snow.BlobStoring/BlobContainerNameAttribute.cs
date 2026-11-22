namespace Snow.BlobStoring;

/// <summary>
/// 指定容器名: 不写时用容器类型名去掉 BlobContainer 后缀
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BlobContainerNameAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">容器名</param>
    public BlobContainerNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// 容器名
    /// </summary>
    public string Name { get; }
}
