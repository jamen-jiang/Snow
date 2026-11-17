namespace Rongia.Kernel.Application.Security;

/// <summary>
/// 当前登录用户(实现由基础设施层提供,如 Sa-Token → 自研会话)
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 用户Id
    /// </summary>
    long UserId { get; }

    /// <summary>
    /// 账号
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// 用户名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 手机号
    /// </summary>
    string Mobile { get; }

    /// <summary>
    /// 邮箱
    /// </summary>
    string Email { get; }

    /// <summary>
    /// 是否超级管理员
    /// </summary>
    bool IsSuperAdmin { get; }

    /// <summary>
    /// 菜单Id列表
    /// </summary>
    IReadOnlySet<long> MenuIds { get; }

    /// <summary>
    /// 许可证列表
    /// </summary>
    IReadOnlySet<string> Licenses { get; }

    /// <summary>
    /// 公司Id列表
    /// </summary>
    IReadOnlySet<long> CompanyIds { get; }

    /// <summary>
    /// 部门Id列表
    /// </summary>
    IReadOnlySet<long> DepartmentIds { get; }

    /// <summary>
    /// 岗位Id列表
    /// </summary>
    IReadOnlySet<long> PositionIds { get; }
}
