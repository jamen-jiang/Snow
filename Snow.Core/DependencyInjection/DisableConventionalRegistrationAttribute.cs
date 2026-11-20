namespace Snow.DependencyInjection;

/// <summary>
/// 标记类型不参与约定注册(对应 ABP DisableConventionalRegistrationAttribute);已显式注册的服务不受影响
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class DisableConventionalRegistrationAttribute : Attribute
{
}
