using Microsoft.Extensions.Logging;
using Snow.Exceptions;
using Snow.Logging;

namespace Snow.Domain.Entities;

/// <summary>
/// 实体不存在异常(对齐 ABP 的 <c>EntityNotFoundException</c>)
/// <para>继承 <see cref="SnowException"/> 并声明 <see cref="IHasHttpStatusCode.HttpStatusCode"/> 为 404,
/// 于是仓储的 <c>GetAsync</c> 抛它时, 异常处理层直接回 404 + <c>EntityNotFound</c>, 而不是 500 + "服务器内部错误"。</para>
/// <para>错误码固定 <c>EntityNotFound</c>, 客户端据此分支处理; 消息里带实体类型与主键, 便于排查。</para>
/// </summary>
public sealed class EntityNotFoundException : SnowException, IHasHttpStatusCode, IHasErrorCode, IHasLogLevel
{
    /// <summary>
    /// 错误码: 客户端据此判断"要查的东西不存在"
    /// </summary>
    public const string ErrorCode = "EntityNotFound";

    /// <summary>
    /// 错误码(实现 <see cref="IHasErrorCode"/>): 固定 <see cref="ErrorCode"/>
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 日志级别(实现 <see cref="IHasLogLevel"/>): 找不到实体是预期内的, 记 Warning
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// 实体类型
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// 查询使用的主键
    /// </summary>
    public object? Id { get; }

    /// <inheritdoc />
    /// <remarks>字面量而不是 ASP.NET 的 <c>StatusCodes</c>: 领域层不该引 ASP.NET。</remarks>
    public int HttpStatusCode => 404;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="id">主键</param>
    public EntityNotFoundException(Type entityType, object? id)
        : base($"找不到实体 {entityType.FullName}, 主键: {id}")
    {
        EntityType = entityType;
        Id = id;
        Code = ErrorCode;
        LogLevel = LogLevel.Warning;
    }

    /// <summary>
    /// 构造器: 只知道实体类型(比如按条件查询没命中, 没有单一主键)
    /// </summary>
    /// <param name="entityType">实体类型</param>
    public EntityNotFoundException(Type entityType)
        : base($"找不到实体 {entityType.FullName}。")
    {
        EntityType = entityType;
        Code = ErrorCode;
        LogLevel = LogLevel.Warning;
    }

    /// <summary>
    /// 构造器: 直接给消息
    /// </summary>
    /// <param name="message">错误消息</param>
    public EntityNotFoundException(string message)
        : base(message)
    {
        EntityType = typeof(object);
        Code = ErrorCode;
        LogLevel = LogLevel.Warning;
    }
}
