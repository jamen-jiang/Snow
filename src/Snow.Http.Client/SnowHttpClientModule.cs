using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.Http.Client;

/// <summary>
/// HTTP 客户端模块(对应 ABP 的 <c>AbpHttpClientModule</c>): 登记 <see cref="IHttpClientFactory"/> 的命名客户端管线,
/// 并按 <see cref="SnowHttpClientOptions"/> 配置默认客户端 <see cref="SnowHttpClientNames.Default"/>
/// <para>引入后即可注入 <see cref="IHttpClientFactory"/> 或注册自己的类型化客户端; 处理器管线(重试、超时、日志)
/// 由 Microsoft.Extensions.Http 统一管理, 避免手搓静态 HttpClient 踩 DNS 与套接字耗尽。</para>
/// <para>选项不读配置节(与 ABP 一致): 宿主在 <c>PreConfigureServices</c> 里
/// <c>Configure&lt;SnowHttpClientOptions&gt;(context.Configuration.GetSection("HttpClient"))</c> 或直接改属性给出。</para>
/// </summary>
public sealed class SnowHttpClientModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 命名客户端管线: 业务侧注入 IHttpClientFactory, 或自己 AddHttpClient<TClient> 注册类型化客户端
        context.Services.AddHttpClient();

        // 默认客户端: 统一带上基础地址、超时、UserAgent 与默认请求头。
        // 选项在工厂回调里取, 宿主后写进容器的配置也算数
        context.Services.AddHttpClient(SnowHttpClientNames.Default, (serviceProvider, client) =>
        {
            SnowHttpClientOptions options = serviceProvider.GetRequiredService<IOptions<SnowHttpClientOptions>>().Value;

            SnowHttpClientOptionApplier.Apply(client, options);
        });

        // 泛型访问器: 注入 ISnowHttpClient<T> 即得按契约推导出的客户端, 新增客户端不必注册
        context.Services.AddSingleton<SnowHttpClientCatalog>();
        context.Services.AddTransient(typeof(ISnowHttpClient<>), typeof(SnowHttpClient<>));
    }
}
