using Snow.Messaging;

namespace Snow.Sample.Web.Behaviors;

/// <summary>
/// 管道行为示例: 给每个命令/查询套一层耗时日志。
/// <para>开放泛型: 一个类型同时覆盖所有 &lt;TRequest, TResponse&gt; 组合, 不需要为每个请求各写一个。</para>
/// <para>它位于处理器外层(先进入、后退出), 若要包住工作单元则在 <see cref="SnowMessagingOptions.Behaviors"/> 中先于工作单元登记 ——
/// 当前模块把工作单元行为注册在最前, 所以本行为实际运行在事务内部。</para>
/// </summary>
/// <typeparam name="TRequest">请求类型</typeparam>
/// <typeparam name="TResponse">返回值类型</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>构造。</summary>
    /// <param name="logger">日志</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string name = typeof(TRequest).Name;
        _logger.LogInformation("消息开始: {Request}", name);
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            TResponse response = await next(cancellationToken);
            _logger.LogInformation(
                "消息完成: {Request} 耗时 {Elapsed} ms",
                name,
                System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            // 不吞异常: 记录后原样抛出, 事务行为才能照常回滚。
            _logger.LogError(ex, "消息失败: {Request}", name);
            throw;
        }
    }
}
