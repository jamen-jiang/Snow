namespace Rongia.Application.Port.Job;

/// <summary>
/// 后台执行器端口
/// <para>应用层用于把命令提交到后台异步执行的抽象,具体实现由基础设施层提供(如 JobRunr)。</para>
/// </summary>
public interface IBackgroundExecutor
{
    /// <summary>
    /// 提交命令到后台执行
    /// </summary>
    /// <param name="command">待执行的命令,需可被序列化</param>
    void Dispatch(Rongia.Messaging.ICommand command);
}