using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.Http;

namespace Snow.Http.Client;

/// <summary>
/// 声明式 HTTP 接口的代理工厂: 按接口上的 <see cref="HttpExchangeAttribute"/> 与
/// <see cref="HttpMethodExchangeAttribute"/> 把方法调用翻译成 HTTP 请求
/// </summary>
public static class SnowHttpServiceProxyFactory
{
    /// <summary>
    /// 为契约接口创建代理
    /// </summary>
    /// <typeparam name="T">契约接口</typeparam>
    /// <param name="serviceProvider">服务提供器</param>
    /// <returns>代理实例</returns>
    public static T Create<T>(IServiceProvider serviceProvider) where T : class
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        T proxy = DispatchProxy.Create<T, SnowHttpServiceProxy<T>>();
        ((SnowHttpServiceProxy<T>)(object)proxy).Initialize(serviceProvider);

        return proxy;
    }

    /// <summary>
    /// 为契约接口创建代理(非泛型入口, 供容器按 Type 登记时使用)
    /// </summary>
    /// <param name="contractType">契约接口类型</param>
    /// <param name="serviceProvider">服务提供器</param>
    /// <returns>代理实例</returns>
    public static object Create(Type contractType, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        MethodInfo method = typeof(SnowHttpServiceProxyFactory)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == nameof(Create) && candidate.IsGenericMethodDefinition && candidate.GetParameters().Length == 1)
            .MakeGenericMethod(contractType);

        return method.Invoke(null, [serviceProvider])!;
    }
}

/// <summary>
/// 声明式 HTTP 接口的运行时实现(DispatchProxy)
/// </summary>
/// <typeparam name="T">契约接口</typeparam>
public class SnowHttpServiceProxy<T> : DispatchProxy where T : class
{
    private IServiceProvider? _serviceProvider;
    private ISnowHttpClient<T>? _client;
    private SnowHttpServiceOptions? _options;

    /// <summary>
    /// 由工厂调用, 注入服务提供器
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    public void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _options = serviceProvider.GetService<IOptions<SnowHttpServiceOptions>>()?.Value;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod is null)
        {
            throw new SnowHttpServiceException("代理收到空方法信息。", HttpMethod.Get, typeof(T).FullName ?? typeof(T).Name);
        }

        RequestContext context = BuildContext(targetMethod, args ?? []);
        Type returnType = targetMethod.ReturnType;

        if (returnType == typeof(Task))
        {
            return SendVoidAsync(context);
        }

        if (returnType == typeof(ValueTask))
        {
            return SendVoidValueAsync(context);
        }

        if (returnType.IsGenericType)
        {
            Type definition = returnType.GetGenericTypeDefinition();
            Type resultType = returnType.GetGenericArguments()[0];

            if (definition == typeof(Task<>))
            {
                return InvokeGenericAsync(nameof(SendTypedAsync), resultType, context);
            }

            if (definition == typeof(ValueTask<>))
            {
                return InvokeGenericAsync(nameof(SendTypedValueAsync), resultType, context);
            }
        }

        throw new SnowHttpServiceException(
            "声明式 HTTP 接口的方法返回类型只支持 Task / Task<T> / ValueTask / ValueTask<T>, 实际是 " + returnType.Name + "。",
            context.Method,
            context.PathAndQuery);
    }

    private object InvokeGenericAsync(string methodName, Type resultType, RequestContext context)
    {
        // 不能用 GetType(): DispatchProxy 生成的子类看不到基类的私有方法
        MethodInfo method = typeof(SnowHttpServiceProxy<T>)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == methodName && candidate.IsGenericMethodDefinition && candidate.GetParameters().Length == 1)
            .MakeGenericMethod(resultType);

        return method.Invoke(this, [context])!;
    }

    private async Task SendVoidAsync(RequestContext context)
    {
        using HttpResponseMessage response = await SendAsync(context).ConfigureAwait(false);
    }

    private async ValueTask SendVoidValueAsync(RequestContext context)
    {
        using HttpResponseMessage response = await SendAsync(context).ConfigureAwait(false);
    }

    private async Task<TResult> SendTypedAsync<TResult>(RequestContext context)
    {
        using HttpResponseMessage response = await SendAsync(context).ConfigureAwait(false);

        return await ReadAsync<TResult>(response, context.CancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<TResult> SendTypedValueAsync<TResult>(RequestContext context)
    {
        return await SendTypedAsync<TResult>(context).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(RequestContext context)
    {
        HttpClient httpClient = GetClient().HttpClient;

        using HttpRequestMessage request = new(context.Method, context.PathAndQuery);

        if (context.Body is not null)
        {
            request.Content = JsonContent.Create(context.Body, context.Body.GetType(), options: SerializerOptions);
        }

        foreach (KeyValuePair<string, string> header in context.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 相对路径此时还没有拼上 BaseAddress, 报错信息里给出完整地址更方便排查
        string url = httpClient.BaseAddress is not null && Uri.TryCreate(httpClient.BaseAddress, context.PathAndQuery, out Uri? absolute)
            ? absolute.ToString()
            : context.PathAndQuery;

        HttpResponseMessage response;
        try
        {
            response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.CancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            throw new SnowHttpServiceException(
                "声明式 HTTP 请求发送失败: " + context.Method + " " + url + "。请检查 BaseAddress 配置与网络。",
                context.Method,
                url,
                innerException: exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            string responseBody = await response.Content.ReadAsStringAsync(context.CancellationToken).ConfigureAwait(false);
            response.Dispose();

            // 响应体是 Snow 的错误信封时, 抛出携带原始错误体的远程调用异常, 让上层按 Code / ValidationErrors 处理
            SnowRemoteServiceErrorResponse? errorResponse = TryReadErrorResponse(responseBody);
            if (errorResponse?.Error is not null)
            {
                throw new SnowRemoteCallException(errorResponse.Error)
                {
                    HttpStatusCode = (int)statusCode,
                };
            }

            if (responseBody.Length > 2000)
            {
                responseBody = responseBody[..2000];
            }

            throw new SnowHttpServiceException(
                "声明式 HTTP 请求返回 " + (int)statusCode + " " + statusCode + ": " + context.Method + " " + url,
                context.Method,
                url,
                statusCode,
                responseBody);
        }

        return response;
    }

    private SnowRemoteServiceErrorResponse? TryReadErrorResponse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SnowRemoteServiceErrorResponse>(body, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<TResult> ReadAsync<TResult>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return default!;
        }

        TResult? result = await response.Content
            .ReadFromJsonAsync<TResult>(SerializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return result!;
    }

    private ISnowHttpClient<T> GetClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        IServiceProvider serviceProvider = _serviceProvider
            ?? throw new SnowHttpServiceException("代理尚未初始化, 请通过 AddSnowHttpServiceClient 登记后从容器解析。", HttpMethod.Get, typeof(T).Name);

        _client = serviceProvider.GetRequiredService<ISnowHttpClient<T>>();

        return _client;
    }

    private JsonSerializerOptions SerializerOptions => (_options ?? SnowHttpServiceOptions.Default).JsonSerializerOptions;

    private RequestContext BuildContext(MethodInfo method, object?[] args)
    {
        MethodInfo exchangeMethod = ResolveExchangeMethod(method);

        Type owner = exchangeMethod.DeclaringType ?? typeof(T);
        string basePath = owner.GetCustomAttribute<HttpExchangeAttribute>(inherit: true)?.Path
            ?? typeof(T).GetCustomAttribute<HttpExchangeAttribute>(inherit: true)?.Path
            ?? string.Empty;

        HttpMethod httpMethod;
        string subPath;

        if (exchangeMethod.GetCustomAttribute<HttpMethodExchangeAttribute>(inherit: true) is HttpMethodExchangeAttribute verbExchange)
        {
            httpMethod = verbExchange.Method;
            subPath = verbExchange.Path;
        }
        else if (exchangeMethod.GetCustomAttribute<HttpExchangeAttribute>(inherit: true) is HttpExchangeAttribute genericExchange
            && !string.IsNullOrWhiteSpace(genericExchange.Method))
        {
            // 方法级 [HttpExchange("/x", Method = "GET")]: 子路径与类型级基路径拼接
            httpMethod = ParseHttpMethod(genericExchange.Method);
            subPath = genericExchange.Path;
        }
        else
        {
            throw new SnowHttpServiceException(
                "方法 " + owner.Name + "." + exchangeMethod.Name + " 缺少 [GetExchange] / [PostExchange] / [PutExchange] / [DeleteExchange] / [PatchExchange] 标注, 或方法级 [HttpExchange(Method = \"GET\")]。",
                HttpMethod.Get,
                typeof(T).Name);
        }

        ParameterInfo[] parameters = exchangeMethod.GetParameters();
        int bodyIndex = FindBodyIndex(parameters);

        object? body = null;
        List<KeyValuePair<string, string>> query = [];
        List<KeyValuePair<string, string>> headers = [];
        Dictionary<string, string> routeValues = new(StringComparer.OrdinalIgnoreCase);
        CancellationToken cancellationToken = CancellationToken.None;

        for (int index = 0; index < parameters.Length; index++)
        {
            ParameterInfo parameter = parameters[index];
            object? value = index < args.Length ? args[index] : null;

            if (parameter.ParameterType == typeof(CancellationToken))
            {
                cancellationToken = value is CancellationToken token ? token : CancellationToken.None;
                continue;
            }

            if (index == bodyIndex)
            {
                body = value;
                continue;
            }

            string name = parameter.Name ?? ("arg" + index.ToString(CultureInfo.InvariantCulture));

            if (parameter.GetCustomAttribute<RouteAttribute>() is RouteAttribute route)
            {
                routeValues[route.Name ?? name] = FormatValue(value);
                continue;
            }

            if (parameter.GetCustomAttribute<HeaderAttribute>() is HeaderAttribute header)
            {
                if (value is not null)
                {
                    headers.Add(new KeyValuePair<string, string>(header.Name ?? name, FormatValue(value)));
                }

                continue;
            }

            if (parameter.GetCustomAttribute<QueryAttribute>() is QueryAttribute explicitQuery)
            {
                AppendQuery(query, explicitQuery.Name ?? name, value);
                continue;
            }

            if (parameter.GetCustomAttribute<BodyAttribute>() is not null)
            {
                // 多个 [Body] 时以最后一个为准, 与常见直觉一致
                body = value;
                continue;
            }

            if (IsSimple(parameter.ParameterType))
            {
                AppendQuery(query, name, value);
                continue;
            }

            throw new SnowHttpServiceException(
                "方法 " + exchangeMethod.Name + " 的复杂参数 " + name + " 请用 [Body] / [Query] / [Route] / [Header] 之一标注。",
                httpMethod,
                CombinePaths(basePath, subPath));
        }

        string path = CombinePaths(basePath, subPath);

        foreach (KeyValuePair<string, string> routeValue in routeValues)
        {
            path = path.Replace("{" + routeValue.Key + "}", Uri.EscapeDataString(routeValue.Value), StringComparison.OrdinalIgnoreCase);
        }

        if (path.Contains('{'))
        {
            throw new SnowHttpServiceException(
                "路径 " + path + " 还有未填充的占位, 请给相应参数加 [Route]。",
                httpMethod,
                path);
        }

        if (query.Count > 0)
        {
            StringBuilder builder = new(path);
            builder.Append(path.Contains('?') ? '&' : '?');

            for (int index = 0; index < query.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append('&');
                }

                builder.Append(Uri.EscapeDataString(query[index].Key)).Append('=').Append(Uri.EscapeDataString(query[index].Value));
            }

            path = builder.ToString();
        }

        return new RequestContext(httpMethod, path, body, headers, cancellationToken);
    }

    private static int FindBodyIndex(ParameterInfo[] parameters)
    {
        for (int index = 0; index < parameters.Length; index++)
        {
            if (parameters[index].GetCustomAttribute<BodyAttribute>() is not null)
            {
                return index;
            }
        }

        int candidateIndex = -1;
        int candidates = 0;

        for (int index = 0; index < parameters.Length; index++)
        {
            ParameterInfo parameter = parameters[index];

            if (parameter.ParameterType == typeof(CancellationToken)
                || parameter.GetCustomAttribute<QueryAttribute>() is not null
                || parameter.GetCustomAttribute<RouteAttribute>() is not null
                || parameter.GetCustomAttribute<HeaderAttribute>() is not null
                || IsSimple(parameter.ParameterType))
            {
                continue;
            }

            candidates++;
            candidateIndex = index;
        }

        return candidates == 1 ? candidateIndex : -1;
    }

    private static MethodInfo ResolveExchangeMethod(MethodInfo method)
    {
        if (method.GetCustomAttribute<HttpMethodExchangeAttribute>(inherit: true) is not null)
        {
            return method;
        }

        Type? declaringType = method.DeclaringType;
        if (declaringType is null)
        {
            return method;
        }

        Type[] signature = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        foreach (Type contract in declaringType.GetInterfaces())
        {
            foreach (MethodInfo inherited in contract.GetMethods())
            {
                if (!string.Equals(inherited.Name, method.Name, StringComparison.Ordinal)
                    || !inherited.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(signature))
                {
                    continue;
                }

                if (inherited.GetCustomAttribute<HttpMethodExchangeAttribute>(inherit: true) is not null)
                {
                    return inherited;
                }
            }
        }

        return method;
    }

    private static HttpMethod ParseHttpMethod(string value)
    {
        string name = value.Trim().ToUpperInvariant();

        return name switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "PATCH" => HttpMethod.Patch,
            "HEAD" => HttpMethod.Head,
            "OPTIONS" => HttpMethod.Options,
            "TRACE" => HttpMethod.Trace,
            _ => throw new SnowHttpServiceException(
                "[HttpExchange(Method = \"" + value + "\")] 不是受支持的 HTTP 动词, 可用 GET / POST / PUT / DELETE / PATCH / HEAD / OPTIONS / TRACE。",
                HttpMethod.Get,
                value),
        };
    }

    private static bool IsSimple(Type type)
    {
        Type target = Nullable.GetUnderlyingType(type) ?? type;

        return target.IsPrimitive
            || target.IsEnum
            || target == typeof(string)
            || target == typeof(decimal)
            || target == typeof(DateTime)
            || target == typeof(DateTimeOffset)
            || target == typeof(DateOnly)
            || target == typeof(TimeOnly)
            || target == typeof(TimeSpan)
            || target == typeof(Guid)
            || target == typeof(Uri);
    }

    private static void AppendQuery(List<KeyValuePair<string, string>> query, string name, object? value)
    {
        switch (value)
        {
            case null:
                return;
            case string text:
                if (text.Length > 0)
                {
                    query.Add(new KeyValuePair<string, string>(name, text));
                }

                return;
            case System.Collections.IEnumerable items:
                foreach (object? item in items)
                {
                    if (item is not null)
                    {
                        query.Add(new KeyValuePair<string, string>(name, FormatValue(item)));
                    }
                }

                return;
            default:
                query.Add(new KeyValuePair<string, string>(name, FormatValue(value)));
                return;
        }
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("O", CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static string CombinePaths(string basePath, string path)
    {
        string head = (basePath ?? string.Empty).TrimEnd('/');
        string tail = (path ?? string.Empty).TrimStart('/');

        if (head.Length == 0)
        {
            return "/" + tail;
        }

        return tail.Length == 0 ? head : head + "/" + tail;
    }

    private sealed record RequestContext(
        HttpMethod Method,
        string PathAndQuery,
        object? Body,
        List<KeyValuePair<string, string>> Headers,
        CancellationToken CancellationToken);
}
