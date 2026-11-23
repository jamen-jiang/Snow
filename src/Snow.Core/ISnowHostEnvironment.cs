namespace Snow;

/// <summary>
/// 宿主环境名(对应 ABP 的 <c>IAbpHostEnvironment</c>): 只暴露一个可读可写的环境名
/// <para>存在的理由: <c>IWebHostEnvironment</c> 只有 Web 宿主才有, 而模块要在服务注册阶段就判断
/// 「当前是不是开发环境」, 那时容器还没建、也不该为了一个字符串去依赖 ASP.NET Core。
/// 所以框架在装配模块之前先放一个可写单例进来, Web 宿主再由
/// <c>SnowAspNetCoreModule</c> 把真实环境名填进去。</para>
/// </summary>
public interface ISnowHostEnvironment
{
    /// <summary>
    /// 环境名, 取值与 <c>Microsoft.Extensions.Hosting.Environments</c> 一致: Development / Staging / Production
    /// </summary>
    string? EnvironmentName { get; set; }
}
