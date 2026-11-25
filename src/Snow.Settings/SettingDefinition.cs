namespace Snow.Settings;

/// <summary>
/// 一个配置项的定义(对应 ABP 的 SettingDefinition): 名字、默认值、是否对客户端可见、是否继承、是否加密等
/// <para>定义由 <see cref="SettingDefinitionProvider"/> 声明, 由 <see cref="SettingDefinitionManager"/> 汇总;
/// 定义本身不存值, 值由 <see cref="ISettingValueProvider"/> 提供。</para>
/// </summary>
public class SettingDefinition
{
    /// <summary>
    /// 配置项名(全局唯一, 约定用 <c>包名.分组.名字</c>)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 所有提供器都给不出值时的默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 显示名, 未本地化(对应 ABP 的 ILocalizableString, Snow 暂无本地化字符串抽象)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否允许下发到客户端
    /// </summary>
    public bool IsVisibleToClients { get; set; }

    /// <summary>
    /// 是否可被上层(租户等)覆盖, 默认 true
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// 值是否加密存储; true 时读取后由 <see cref="ISettingEncryptionService"/> 解密
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// 只由这些提供器(按名字)取值; 空表示所有提供器都可以
    /// </summary>
    public List<string> Providers { get; }

    /// <summary>
    /// 附加属性
    /// </summary>
    public Dictionary<string, object?> Properties { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">配置项名</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="displayName">显示名</param>
    /// <param name="description">说明</param>
    /// <param name="isVisibleToClients">是否对客户端可见</param>
    /// <param name="isInherited">是否可被覆盖</param>
    /// <param name="isEncrypted">是否加密存储</param>
    public SettingDefinition(
        string name,
        string? defaultValue = null,
        string? displayName = null,
        string? description = null,
        bool isVisibleToClients = false,
        bool isInherited = true,
        bool isEncrypted = false)
    {
        Name = name;
        DefaultValue = defaultValue;
        DisplayName = displayName;
        Description = description;
        IsVisibleToClients = isVisibleToClients;
        IsInherited = isInherited;
        IsEncrypted = isEncrypted;
        Providers = new List<string>();
        Properties = new Dictionary<string, object?>();
    }

    /// <summary>
    /// 追加/覆盖一个附加属性
    /// </summary>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <returns>自身, 便于链式写法</returns>
    public SettingDefinition WithProperty(string key, object? value)
    {
        Properties[key] = value;
        return this;
    }
}
