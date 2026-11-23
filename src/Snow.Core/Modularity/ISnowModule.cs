namespace Snow.Modularity;

/// <summary>
/// 模块接口(对应 ABP 的 <c>IAbpModule</c>, 成员逐个对齐): 只声明服务注册阶段的两个方法
/// <para>判定「这是不是一个模块」只看这个接口, 与 ABP 的 <c>AbpModule.IsAbpModule</c> 判 <c>IAbpModule</c> 同理 ——
/// 所以启动模块的泛型约束是 <c>where TStartupModule : ISnowModule</c>, 不要求继承 <see cref="SnowModule"/>。</para>
/// <para>前段 <c>PreConfigureServices</c> / 后段 <c>PostConfigureServices</c> 不在这里: ABP 把它们放在
/// <c>IPreConfigureServices</c> / <c>IPostConfigureServices</c> 两个独立接口上, 由 <see cref="SnowModule"/> 一并实现。</para>
/// </summary>
public interface ISnowModule
{
    /// <summary>
    /// 服务注册中段的异步版本
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    /// <returns>表示注册过程的任务</returns>
    Task ConfigureServicesAsync(ServiceConfigurationContext context);

    /// <summary>
    /// 服务注册中段
    /// </summary>
    /// <param name="context">服务配置上下文</param>
    void ConfigureServices(ServiceConfigurationContext context);
}
