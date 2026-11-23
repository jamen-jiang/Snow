namespace Snow.Authentication;

/// <summary>
/// 把配置翻译成密钥
/// <para>换成从 KeyVault / 环境变量 / KMS 取密钥, 实现这个接口即可, 其余代码不用动。</para>
/// </summary>
public interface ITokenKeyProvider
{
    /// <summary>
    /// 解析密钥; 配置有错直接抛 <see cref="InvalidOperationException"/>, 在启动阶段就暴露
    /// </summary>
    TokenKeySet Resolve(SnowAuthenticationOptions options);
}
