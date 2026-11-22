namespace Snow.Http.Client;

/// <summary>
/// HTTP 交换地址(对应 Spring 的 <c>@HttpExchange</c>)。打在接口上是基路径, 与方法的
/// <see cref="HttpMethodExchangeAttribute"/> 拼成完整地址; 也可以直接打在方法上并指定
/// <see cref="Method"/>, 等价于 Spring 的 <c>@HttpExchange(method = "GET", path = "...")</c>
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HttpExchangeAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="path">接口上是基路径(如 <c>/api/BomOrder</c>), 方法上是子路径</param>
    public HttpExchangeAttribute(string path = "")
    {
        Path = path ?? string.Empty;
    }

    /// <summary>
    /// 路径: 打在接口上时是基路径, 打在方法上是子路径
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// HTTP 动词(如 <c>GET</c> / <c>POST</c>): 只有打在方法上时生效, 打在接口上时忽略
    /// </summary>
    public string? Method { get; set; }
}

/// <summary>
/// 方法级 HTTP 动词与子路径的基类; 请使用 <see cref="GetExchangeAttribute"/>、
/// <see cref="PostExchangeAttribute"/>、<see cref="PutExchangeAttribute"/>、
/// <see cref="DeleteExchangeAttribute"/>、<see cref="PatchExchangeAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public abstract class HttpMethodExchangeAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="method">HTTP 动词</param>
    /// <param name="path">子路径, 可含 <c>{名字}</c> 占位</param>
    protected HttpMethodExchangeAttribute(HttpMethod method, string path)
    {
        Method = method;
        Path = path ?? string.Empty;
    }

    /// <summary>
    /// HTTP 动词
    /// </summary>
    public HttpMethod Method { get; }

    /// <summary>
    /// 子路径
    /// </summary>
    public string Path { get; }
}

/// <summary>
/// GET 请求(对应 <c>@GetExchange</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class GetExchangeAttribute : HttpMethodExchangeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="path">子路径</param>
    public GetExchangeAttribute(string path = "")
        : base(HttpMethod.Get, path)
    {
    }
}

/// <summary>
/// POST 请求(对应 <c>@PostExchange</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PostExchangeAttribute : HttpMethodExchangeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="path">子路径</param>
    public PostExchangeAttribute(string path = "")
        : base(HttpMethod.Post, path)
    {
    }
}

/// <summary>
/// PUT 请求(对应 <c>@PutExchange</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PutExchangeAttribute : HttpMethodExchangeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="path">子路径</param>
    public PutExchangeAttribute(string path = "")
        : base(HttpMethod.Put, path)
    {
    }
}

/// <summary>
/// DELETE 请求(对应 <c>@DeleteExchange</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DeleteExchangeAttribute : HttpMethodExchangeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="path">子路径</param>
    public DeleteExchangeAttribute(string path = "")
        : base(HttpMethod.Delete, path)
    {
    }
}

/// <summary>
/// PATCH 请求(对应 <c>@PatchExchange</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PatchExchangeAttribute : HttpMethodExchangeAttribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="path">子路径</param>
    public PatchExchangeAttribute(string path = "")
        : base(HttpMethod.Patch, path)
    {
    }
}

/// <summary>
/// 标注该参数序列化成请求体(对应 <c>@RequestBody</c>)。
/// 不标时: 方法只有一个复杂类型的自由参数, 也按请求体处理
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class BodyAttribute : Attribute;

/// <summary>
/// 标注该参数拼到查询串(对应 <c>@RequestParam</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class QueryAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">查询参数名, 不填用形参名</param>
    public QueryAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// 查询参数名
    /// </summary>
    public string? Name { get; }
}

/// <summary>
/// 标注该参数填到路径占位 <c>{名字}</c>(对应 <c>@PathVariable</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class RouteAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">占位名, 不填用形参名</param>
    public RouteAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// 占位名
    /// </summary>
    public string? Name { get; }
}

/// <summary>
/// 标注该参数作为请求头(对应 <c>@RequestHeader</c>)
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class HeaderAttribute : Attribute
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="name">请求头名, 不填用形参名</param>
    public HeaderAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// 请求头名
    /// </summary>
    public string? Name { get; }
}
