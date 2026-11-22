namespace Snow.Http.Client;

/// <summary>
/// 泛型客户端访问器: 注入 <c>ISnowHttpClient&lt;IHkoTrackingClient&gt;</c> 即得已配好基础地址、超时、
/// UserAgent 与默认请求头的 <see cref="HttpClient"/>, 新增客户端不需要写任何注册代码
/// <para>选项来源: 配置节 <c>HttpClient:{名字}</c>(名字由契约类型推导), 缺省回退 <c>HttpClient</c> 的默认值。</para>
/// </summary>
/// <typeparam name="T">客户端契约(接口)类型; 请用接口, 用实现类会要求容器能构造它</typeparam>
/// <remarks>释放访问器即释放它持有的 <see cref="HttpClient"/>, 建议 <c>using</c> 取用。</remarks>
public interface ISnowHttpClient<T> : IDisposable where T : class
{
    /// <summary>
    /// 命名客户端名称(由契约类型推导, 见 <see cref="SnowHttpClientNames.For(Type)"/>)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 已按选项配好的客户端; 与访问器同生命周期, 访问器释放时一并释放
    /// </summary>
    HttpClient HttpClient { get; }

    /// <summary>
    /// 本次生效的选项
    /// </summary>
    SnowHttpClientOptions Options { get; }
}
