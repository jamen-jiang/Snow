namespace Snow.Validation.FluentValidation;

/// <summary>
/// FluentValidation 集成选项
/// </summary>
public sealed class SnowFluentValidationOptions
{
    /// <summary>
    /// 总开关, 关闭后不扫描校验器也不接进校验管线
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否扫描模块程序集里所有 <c>AbstractValidator&lt;T&gt;</c> 实现并登记
    /// </summary>
    public bool ScanValidators { get; set; } = true;

    /// <summary>
    /// 是否仍然执行 DataAnnotations 特性校验(默认执行, 两者叠加)
    /// </summary>
    public bool ValidateDataAnnotations { get; set; } = true;

    /// <summary>
    /// 额外登记的校验器类型: 不想让扫描扫到的实现可在这里点名
    /// </summary>
    public IList<Type> ValidatorTypes { get; } = new List<Type>();
}
