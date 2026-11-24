using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Snow.DependencyInjection;

namespace Snow.ExceptionHandling;

/// <summary>
/// 默认异常通知器(对应 ABP 的 <c>ExceptionNotifier</c>): 开一个作用域逐个调用 <see cref="IExceptionSubscriber"/>,
/// 单个订阅者抛错不影响其余订阅者。
/// </summary>
[TransientService]
public class ExceptionNotifier : IExceptionNotifier
{
    /// <summary>
    /// 日志
    /// </summary>
    public ILogger<ExceptionNotifier> Logger { get; set; }

    /// <summary>
    /// 作用域工厂: 订阅者从独立作用域解析, 生命周期互不影响
    /// </summary>
    protected IServiceScopeFactory ServiceScopeFactory { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="serviceScopeFactory">作用域工厂</param>
    public ExceptionNotifier(IServiceScopeFactory serviceScopeFactory)
    {
        ServiceScopeFactory = serviceScopeFactory;
        Logger = NullLogger<ExceptionNotifier>.Instance;
    }

    /// <inheritdoc />
    /// <param name="context">上下文</param>
    public virtual async Task NotifyAsync([NotNull] ExceptionNotificationContext context)
    {
        Check.NotNull(context, nameof(context));

        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var exceptionSubscribers = scope.ServiceProvider.GetServices<IExceptionSubscriber>();

            foreach (var exceptionSubscriber in exceptionSubscribers)
            {
                try
                {
                    await exceptionSubscriber.HandleAsync(context);
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Exception subscriber of type {exceptionSubscriber.GetType().AssemblyQualifiedName} has thrown an exception!");
                    Logger.LogException(e, LogLevel.Warning);
                }
            }
        }
    }
}
