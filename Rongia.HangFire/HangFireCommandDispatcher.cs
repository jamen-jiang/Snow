using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Rongia.DependencyInjection;
using Rongia.Messaging;

namespace Rongia.HangFire;

/// <summary>
/// 后台命令派发入口: 由 <see cref="HangFireBackgroundExecutor"/> 投递, 回放时反序列化并按类型调用 <see cref="ICommandBus"/>
/// </summary>
public class HangFireCommandDispatcher : ITransientDependency
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> _sendMethods = new();

    private readonly ICommandBus _commandBus;

    private readonly RongiaHangFireJobTypeRegistry _registry;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="commandBus">命令总线</param>
    /// <param name="registry">类型表</param>
    public HangFireCommandDispatcher(ICommandBus commandBus, RongiaHangFireJobTypeRegistry registry)
    {
        _commandBus = commandBus;
        _registry = registry;
    }

    /// <summary>
    /// 反序列化命令并派发
    /// </summary>
    /// <param name="commandTypeName">命令类型全名</param>
    /// <param name="payload">命令 JSON</param>
    public async Task DispatchAsync(string commandTypeName, string payload)
    {
        Type type = _registry.Resolve(commandTypeName);
        object? command = JsonSerializer.Deserialize(payload, type)
            ?? throw new InvalidOperationException($"命令反序列化结果为空, 类型: {commandTypeName}。");

        if (command is not ICommand)
        {
            throw new InvalidOperationException($"类型 {commandTypeName} 不是 ICommand, 无法在后台派发。");
        }

        MethodInfo sendMethod = _sendMethods.GetOrAdd(type, static commandType => typeof(ICommandBus)
            .GetMethods()
            .Single(method => method.Name == nameof(ICommandBus.SendAsync) && method.IsGenericMethodDefinition && method.GetParameters().Length == 2 && method.ReturnType == typeof(Task))
            .MakeGenericMethod(commandType));

        object? result = sendMethod.Invoke(_commandBus, [command, CancellationToken.None]);

        if (result is Task task)
        {
            await task.ConfigureAwait(false);
        }
    }
}
