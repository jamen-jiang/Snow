using Snow.DependencyInjection;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限定义提供器基类: 只覆写 <see cref="Define"/> 即可
/// </summary>
public abstract class PermissionDefinitionProvider : IPermissionDefinitionProvider, ITransientDependency
{
    /// <inheritdoc />
    public virtual void PreDefine(IPermissionDefinitionContext context)
    {
    }

    /// <inheritdoc />
    public abstract void Define(IPermissionDefinitionContext context);

    /// <inheritdoc />
    public virtual void PostDefine(IPermissionDefinitionContext context)
    {
    }
}
