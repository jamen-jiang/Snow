using Snow.Exceptions;

namespace Snow;

/// <summary>
/// 用户友好异常标记接口(对应 ABP 的 <c>IUserFriendlyException</c>)
/// <para>继承 <see cref="IBusinessException"/>: 表示这次失败的消息本身就可以直接展示给调用方, 不必脱敏。</para>
/// </summary>
public interface IUserFriendlyException : IBusinessException
{
}
