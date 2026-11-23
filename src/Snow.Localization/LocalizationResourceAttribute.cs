namespace Snow.Localization;

/// <summary>
/// 标注类型所属的本地化资源名
/// <para>不标注时用类型的完整名称作为资源名; 注入 <c>IStringLocalizer&lt;T&gt;</c> 即按此规则找资源。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class LocalizationResourceAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">资源名</param>
    public LocalizationResourceAttribute(string name) => Name = name;

    /// <summary>
    /// 资源名
    /// </summary>
    public string Name { get; }
}
