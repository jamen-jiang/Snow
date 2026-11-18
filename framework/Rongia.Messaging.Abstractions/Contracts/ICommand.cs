namespace Rongia.Messaging;

/// <summary>
/// 无返回值命令的标记接口(对应写操作)
/// </summary>
public interface ICommand
{
}

/// <summary>
/// 有返回值命令的标记接口
/// </summary>
/// <typeparam name="TResponse">返回值类型</typeparam>
/// <remarks>
/// 有返回值与无返回值是两个兄弟接口,互不继承:否则 <see cref="ICommandBus"/> 的重载会在调用处出现二义性。
/// </remarks>
public interface ICommand<out TResponse>
{
}
