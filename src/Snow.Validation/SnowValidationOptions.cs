namespace Snow.Validation;

/// <summary>
/// 校验选项
/// </summary>
public sealed class SnowValidationOptions
{
    /// <summary>
    /// 总开关, 关闭后 [Validation] 不做任何检查
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否递归校验嵌套的复杂对象
    /// </summary>
    public bool ValidateNestedObjects { get; set; } = true;

    /// <summary>
    /// 递归深度上限, 防止环形引用
    /// </summary>
    public int MaxDepth { get; set; } = 4;
}
