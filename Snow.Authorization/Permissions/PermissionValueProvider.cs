using Snow.DependencyInjection;

namespace Snow.Authorization.Permissions;

/// <summary>
/// 权限值提供器基类: 注入 <see cref="IPermissionStore"/> 供子类查询授权数据
/// </summary>
[TransientService]
public abstract class PermissionValueProvider : IPermissionValueProvider
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="permissionStore">权限存储</param>
    protected PermissionValueProvider(IPermissionStore permissionStore)
    {
        ArgumentNullException.ThrowIfNull(permissionStore);
        PermissionStore = permissionStore;
    }

    /// <summary>
    /// 权限存储
    /// </summary>
    protected IPermissionStore PermissionStore { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context);

    /// <inheritdoc />
    public abstract Task<MultiplePermissionGrantResult> CheckAsync(PermissionValuesCheckContext context);
}
