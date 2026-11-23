namespace Snow.Settings;

/// <summary>
/// 设置键值对
/// </summary>
/// <param name="Name">设置名</param>
/// <param name="Value">设置值</param>
public sealed record SettingValue(string Name, string? Value);
