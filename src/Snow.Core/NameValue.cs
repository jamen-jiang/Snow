using System;

namespace Snow;

/// <summary>
/// 名/值(键/值)对(对应 ABP 的 <c>NameValue</c>), 用于时区列表等名称与值成对出现的场景
/// </summary>
[Serializable]
public class NameValue : NameValue<string>
{
    /// <summary>
    /// 构造器
    /// </summary>
    public NameValue()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="value">值</param>
    public NameValue(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>
/// 泛型名/值对
/// </summary>
/// <typeparam name="T">值的类型</typeparam>
[Serializable]
public class NameValue<T>
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 值
    /// </summary>
    public T Value { get; set; } = default!;

    /// <summary>
    /// 构造器
    /// </summary>
    public NameValue()
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">名称</param>
    /// <param name="value">值</param>
    public NameValue(string name, T value)
    {
        Name = name;
        Value = value;
    }
}
