using System.Text.Json;
using Rongia.Application.Port.Job;
using Rongia.Messaging;
using global::Hangfire;

namespace Rongia.HangFire;

/// <summary>
/// <see cref="IBackgroundExecutor"/> 的 Hangfire 实现: 把命令序列化后投递到后台队列
/// <para>命令按运行时类型序列化, 避免 Hangfire 默认序列化器对接口参数丢类型信息</para>
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
    public void Dispatch(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        Type commandType = command.GetType();
        string typeName = commandType.FullName ?? commandType.Name;
        string payload = JsonSerializer.Serialize(command, commandType);

        _backgroundJobClient.Enqueue<HangFireCommandDispatcher>(dispatcher => dispatcher.DispatchAsync(typeName, payload));
    }
}
