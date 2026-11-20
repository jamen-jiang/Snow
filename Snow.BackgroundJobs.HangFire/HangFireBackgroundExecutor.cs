using System.Text.Json;
using Snow.BackgroundJobs;
using global::Hangfire;

namespace Snow.BackgroundJobs.HangFire;

/// <summary>
/// <see cref="IBackgroundExecutor"/> 的 Hangfire 实现: 把载荷序列化后投递到后台队列
/// <para>载荷按泛型参数给出的类型序列化, 避免 Hangfire 默认序列化器对接口参数丢类型信息</para>
/// </summary>
public class HangFireBackgroundExecutor : IBackgroundExecutor
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="backgroundJobClient">即时任务客户端</param>
    public HangFireBackgroundExecutor(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    /// <inheritdoc />
    public Task DispatchAsync<TCommand>(TCommand payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        Type commandType = typeof(TCommand);
        string typeName = commandType.FullName ?? commandType.Name;
        string json = JsonSerializer.Serialize(payload, commandType);

        _backgroundJobClient.Enqueue<HangFireCommandDispatcher>(dispatcher => dispatcher.DispatchAsync(typeName, json));

        return Task.CompletedTask;
    }
}
