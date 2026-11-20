namespace Snow.Authorization.Permissions;

/// <summary>
/// 批量权限授予结果
/// </summary>
public sealed class MultiplePermissionGrantResult
{
    /// <summary>
    /// 权限名 → 授予结果
    /// </summary>
    public Dictionary<string, PermissionGrantResult> Result { get; }

    /// <summary>
    /// 是否全部已授予
    /// </summary>
    public bool AllGranted => Result.Count > 0 && Result.Values.All(value => value == PermissionGrantResult.Granted);

    /// <summary>
    /// 是否全部已禁止
    /// </summary>
    public bool AllProhibited => Result.Count > 0 && Result.Values.All(value => value == PermissionGrantResult.Prohibited);

    /// <summary>
    /// 构造器: 空结果
    /// </summary>
    public MultiplePermissionGrantResult()
    {
        Result = new Dictionary<string, PermissionGrantResult>(StringComparer.Ordinal);
    }

    /// <summary>
    /// 构造器: 以给定初值填充全部权限名
    /// </summary>
    /// <param name="names">权限名集合</param>
    /// <param name="grantResult">初始结果</param>
    public MultiplePermissionGrantResult(string[] names, PermissionGrantResult grantResult = PermissionGrantResult.Undefined)
        : this()
    {
        ArgumentNullException.ThrowIfNull(names);

        foreach (string name in names)
        {
            Result[name] = grantResult;
        }
    }

    /// <summary>
    /// 按权限名取结果, 不存在时返回 <see cref="PermissionGrantResult.Undefined"/>
    /// </summary>
    /// <param name="name">权限名</param>
    public PermissionGrantResult this[string name]
        => Result.TryGetValue(name, out PermissionGrantResult value) ? value : PermissionGrantResult.Undefined;
}
