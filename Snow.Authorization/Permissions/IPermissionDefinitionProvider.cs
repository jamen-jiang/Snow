namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限定义提供器: 各模块实现它来声明自己的权限
/// </summary>
public interface IPermissionDefinitionProvider
{
    /// <summary>
    /// 定义前置处理
    /// </summary>
    /// <param name="context">定义上下文</param>
    void PreDefine(IPermissionDefinitionContext context);

    /// <summary>
    /// 定义权限
    /// </summary>
    /// <param name="context">定义上下文</param>
    void Define(IPermissionDefinitionContext context);

    /// <summary>
    /// 定义后置处理
    /// </summary>
    /// <param name="context">定义上下文</param>
    void PostDefine(IPermissionDefinitionContext context);
}
