using Snow.DependencyInjection;
using Snow.Domain.Entities.Auditing;
using Snow.Security.Users;
using Snow.Timing;

namespace Snow.Auditing;

/// <summary>
/// 实体审计字段填充器默认实现
/// <para>只认 <c>Snow.Domain.Entities.Auditing</c> 里的契约接口, 没有实现的实体会被原样放过。</para>
/// </summary>
[TransientService]
public sealed class AuditPropertySetter : IAuditPropertySetter
{
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="clock">时钟</param>
    /// <param name="currentUser">当前用户</param>
    public AuditPropertySetter(IClock clock, ICurrentUser currentUser)
    {
        _clock = clock;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public void SetCreationProperties(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IHasCreationTime hasCreationTime)
        {
            hasCreationTime.CreationTime = _clock.Now;
        }

        if (entity is ICreationAudited creationAudited)
        {
            creationAudited.CreatorId = _currentUser.Id ?? 0;
            creationAudited.Creator = _currentUser.UserName ?? string.Empty;
        }
    }

    /// <inheritdoc />
    public void SetModificationProperties(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IHasModificationTime hasModificationTime)
        {
            hasModificationTime.LastModificationTime = _clock.Now;
        }

        if (entity is IModificationAudited modificationAudited)
        {
            modificationAudited.LastModifierId = _currentUser.Id;
            modificationAudited.LastModifier = _currentUser.UserName;
        }
    }

    /// <inheritdoc />
    public void SetDeletionProperties(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is IDeletionAudited deletionAudited)
        {
            deletionAudited.DeletionTime = _clock.Now;
            deletionAudited.DeleterId = _currentUser.Id;
            deletionAudited.Deleter = _currentUser.UserName;
        }

        if (entity is ISoftDelete softDelete)
        {
            softDelete.IsDeleted = true;
        }
    }
}
