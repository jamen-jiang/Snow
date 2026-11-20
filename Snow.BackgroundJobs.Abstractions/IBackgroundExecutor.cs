namespace Snow.BackgroundJobs;

/// <summary>
/// 后台执行端口: 应用层把一次性的工作丢到后台异步处理
/// <para>默认实现只记日志不投递; 引入 Hangfire 后由 <c>Snow.BackgroundJobs.HangFire</c> 接管。</para>
/// <para>契约层不引用任何消息类型: 执行器只负责把一个对象序列化后投递, 它是不是命令由消费端判定。</para>
/// </summary>
public interface IBackgroundExecutor
{
    /// <summary>
    /// 投递到后台执行
    /// </summary>
    /// <typeparam name="TCommand">载荷类型。用泛型参数而不是 object 入参, 是为了序列化时直接拿到编译期类型, 不必在运行时反射</typeparam>
    /// <param name="payload">待执行的载荷, 需可被序列化</param>
    /// <returns>表示投递过程的任务; 投递完成即返回, 不等待后台执行结果</returns>
    Task DispatchAsync<TCommand>(TCommand payload);
}
