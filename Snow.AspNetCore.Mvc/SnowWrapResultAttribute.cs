namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 控制单个类或方法是否包装统一响应信封
/// <para>不写时按 <see cref="SnowMvcOptions.WrapResultByDefault"/> 决定。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SnowWrapResultAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="wrapResult">true 包装, false 原样返回</param>
    public SnowWrapResultAttribute(bool wrapResult = true)
    {
        WrapResult = wrapResult;
    }

    /// <summary>
    /// 是否包装
    /// </summary>
    public bool WrapResult { get; }
}
