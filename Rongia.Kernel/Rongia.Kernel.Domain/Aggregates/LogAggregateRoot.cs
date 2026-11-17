using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Rongia.Kernel.Domain.Aggregates;

namespace Rongia.Kernel.Domain.Aggregates;

/// <summary>
/// 日志聚合根基类(对应 Java LogAggregateRoot)
/// </summary>
public abstract class LogAggregateRoot : DeleteAggregateRoot
{
    /// <summary>
    /// 用户Id(可空)
    /// </summary>
    [Column("UserId")]
    public long? UserId { get; protected set; }

    /// <summary>
    /// 用户名称(可空,nvarchar(50))
    /// </summary>
    [Column("UserName"), MaxLength(50)]
    public string? UserName { get; protected set; }

    /// <summary>
    /// 开始时间(必填)
    /// </summary>
    [Column("StartedOn")]
    public DateTime StartedOn { get; protected set; }

    /// <summary>
    /// 结束时间(必填)
    /// </summary>
    [Column("EndedOn")]
    public DateTime EndedOn { get; protected set; }

    /// <summary>
    /// 耗时(毫秒,必填)
    /// </summary>
    [Column("ElapsedMilliseconds")]
    public long ElapsedMilliseconds { get; protected set; }

    /// <summary>
    /// 请求数据(可空)
    /// </summary>
    [Column("Request")]
    public string? Request { get; protected set; }

    /// <summary>
    /// 返回数据(可空)
    /// </summary>
    [Column("Response")]
    public string? Response { get; protected set; }

    /// <summary>
    /// 是否成功(必填)
    /// </summary>
    [Column("IsSuccess")]
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// 错误信息(可空)
    /// </summary>
    [Column("ErrorMessage")]
    public string? ErrorMessage { get; protected set; }

    /// <summary>
    /// 异常信息(可空)
    /// </summary>
    [Column("Exception")]
    public string? Exception { get; protected set; }

    /// <summary>
    /// 无参构造器(供框架反射使用)
    /// </summary>
    protected LogAggregateRoot() { }

    /// <summary>
    /// 构造器。userId 为 0 时视为 null(同时清空 userName),与 Java 侧行为一致。
    /// </summary>
    protected LogAggregateRoot(
        long? userId,
        string? userName,
        DateTime startedOn,
        DateTime endedOn,
        long elapsedMilliseconds,
        string? request,
        string? response,
        bool isSuccess,
        string? errorMessage,
        string? exception)
    {
        if (userId is 0L)
        {
            UserId = null;
            UserName = null;
        }
        else
        {
            UserId = userId;
            UserName = userName;
        }
        StartedOn = startedOn;
        EndedOn = endedOn;
        ElapsedMilliseconds = elapsedMilliseconds;
        Request = request;
        Response = response;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Exception = exception;
    }
}
