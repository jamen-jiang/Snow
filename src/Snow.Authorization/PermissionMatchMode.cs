namespace Snow.Authorization;

/// <summary>
/// 多个权限的满足方式
/// </summary>
public enum PermissionMatchMode
{
    /// <summary>
    /// 拥有其中任意一个即通过(默认)
    /// </summary>
    Any = 0,

    /// <summary>
    /// 每一个都必须拥有
    /// </summary>
    All = 1,
}
