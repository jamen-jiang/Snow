namespace Snow.Exceptions;

/// <summary>
/// 实体不存在异常(泛型版, 对齐 ABP 11 的 <c>EntityNotFoundException&lt;TEntityType&gt;</c>):
/// 实体类型由类型参数给出, 调用方不用再写 <c>typeof(T)</c>。
/// </summary>
/// <typeparam name="TEntityType">实体类型</typeparam>
public class EntityNotFoundException<TEntityType> : EntityNotFoundException
{
    /// <summary>
    /// 构造器: 只知道实体类型
    /// </summary>
    public EntityNotFoundException()
        : base(typeof(TEntityType))
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="id">主键</param>
    public EntityNotFoundException(object? id)
        : base(typeof(TEntityType), id)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="innerException">内部异常</param>
    public EntityNotFoundException(object? id, Exception? innerException)
        : base(typeof(TEntityType), id, innerException)
    {
    }
}

/// <summary>
/// 实体不存在异常(对齐 ABP 11 的 <c>Volo.Abp.Domain.Entities.EntityNotFoundException</c>):
/// 期望能找到实体却没找到时抛。
/// <para>与 ABP 一样是普通异常, 不带错误码/状态码实现 —— 下发成 404 靠异常处理层的类型判断
/// (<c>DefaultHttpExceptionStatusCodeFinder</c> 与 <c>DefaultExceptionToErrorInfoConverter</c>),
/// 类型本身只负责携带实体类型与主键。</para>
/// </summary>
public class EntityNotFoundException : SnowException
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public Type? EntityType { get; set; }

    /// <summary>
    /// 查询使用的主键
    /// </summary>
    public object? Id { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public EntityNotFoundException()
    {
    }

    /// <summary>
    /// 构造器: 只知道实体类型(比如按条件查询没命中, 没有单一主键)
    /// </summary>
    /// <param name="entityType">实体类型</param>
    public EntityNotFoundException(Type entityType)
        : this(entityType, null, null)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="id">主键</param>
    public EntityNotFoundException(Type entityType, object? id)
        : this(entityType, id, null)
    {
    }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="id">主键</param>
    /// <param name="innerException">内部异常</param>
    public EntityNotFoundException(Type entityType, object? id, Exception? innerException)
        : base(id == null
            ? $"不存在该实体。实体类型: {entityType.FullName}"
            : $"不存在该实体。实体类型: {entityType.FullName}, 主键: {id}", innerException)
    {
        EntityType = entityType;
        Id = id;
    }

    /// <summary>
    /// 构造器: 直接给消息
    /// </summary>
    /// <param name="message">错误消息</param>
    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 构造器: 直接给消息与内部异常
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
