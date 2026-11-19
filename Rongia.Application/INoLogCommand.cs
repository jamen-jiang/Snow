namespace Rongia.Application;

/// <summary>
/// 无日志命令标记接口(命令日志拦截器跳过该类命令, 防止递归记录)
/// </summary>
public interface INoLogCommand : Rongia.Messaging.ICommand
{
}
