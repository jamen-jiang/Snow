using Snow.Modularity;
using Snow.Security;

namespace Snow.Settings;

/// <summary>
/// 配置项模块: 注册定义、读取与密码解密实现; 具体配置项由各能力包声明
/// </summary>
[DependsOn(typeof(SnowSecurityModule))]
public sealed class SnowSettingsModule : SnowModule
{
}
