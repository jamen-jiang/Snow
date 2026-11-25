namespace Snow.ExceptionHandling;

/// <summary>
/// 异常本地化选项(对应 ABP 的 <c>AbpExceptionLocalizationOptions</c>)
/// </summary>
public class SnowExceptionLocalizationOptions
{
    /// <summary>
    /// 错误码命名空间 → 本地化资源类型
    /// </summary>
    public Dictionary<string, Type> ErrorCodeNamespaceMappings { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowExceptionLocalizationOptions()
    {
        ErrorCodeNamespaceMappings = new Dictionary<string, Type>();
    }

    /// <summary>
    /// 登记错误码命名空间对应的资源类型
    /// </summary>
    /// <param name="errorCodeNamespace">错误码命名空间</param>
    /// <param name="type">本地化资源类型</param>
    public void MapCodeNamespace(string errorCodeNamespace, Type type)
    {
        ErrorCodeNamespaceMappings[errorCodeNamespace] = type;
    }
}
