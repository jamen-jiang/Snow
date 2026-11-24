namespace Snow;

/// <summary>
/// 标记接口: 实现了它的类型才会被约定路由当成远程服务(对应 ABP 的 <c>IRemoteService</c>)。
/// <para>它只表示"是否套用约定路由", 不会让普通类变成 Controller。</para>
/// </summary>
public interface IRemoteService
{
}