namespace Rongia.Security.SaToken;

/// <summary>
/// 缺少权限异常
/// </summary>
public sealed class SaNotPermissionException : SaTokenException
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissions">缺失的权限标识</param>
    public SaNotPermissionException(params string[] permissions)
        : base($"缺少权限: {string.Join(", ", permissions)}") => Permissions = permissions;

    /// <summary>
    /// 缺失的权限标识
    /// </summary>
    public string[] Permissions { get; }
}
