namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限定义提供器: 继承它并在 <see cref="Define"/> 里登记本模块的权限
/// <para>实现类不需要打生命周期注解, 也不需要注册: 模块会扫描全部程序集里它的子类并登记。</para>
/// </summary>
public abstract class PermissionDefinitionProvider
{
    /// <summary>
    /// 登记权限定义
    /// </summary>
    /// <param name="context">定义上下文</param>
    public abstract void Define(PermissionDefinitionContext context);
}
