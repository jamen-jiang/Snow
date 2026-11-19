namespace Rongia.Messaging;

/// <summary>
/// 查询标记接口(只读语义,必须带返回值)
/// </summary>
/// <typeparam name="TResponse">返回值类型</typeparam>
/// <remarks>与 <see cref="ICommand{TResponse}"/> 平级,不继承命令——查询不能被当成命令发送。</remarks>
public interface IQuery<out TResponse>
{
}
