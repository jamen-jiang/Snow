using System.Threading;
using System.Threading.Tasks;

namespace Snow.BackgroundJobs;

/// <summary>
/// 任务执行器: 还原类型与方法、反序列化参数并调用
/// <para>做成可注入的服务而不是静态类, 才能被替换、被 mock, 也才能套上日志/告警/指标等横切逻辑。</para>
/// </summary>
public interface IBackgroundJobInvoker
{
    /// <summary>
    /// 执行一条已排队的任务记录
    /// </summary>
    /// <param name="jobInfo">任务记录</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RunAsync(BackgroundJobInfo jobInfo, CancellationToken cancellationToken = default);
}
