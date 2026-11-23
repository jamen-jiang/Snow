namespace Snow.Http.Client;

/// <summary>
/// 命名客户端的名称约定: 默认客户端固定为 <see cref="Default"/>, 其余按契约类型推导,
/// 让 <see cref="ISnowHttpClient{T}"/> 不必逐个登记就能找到自己的配置节
/// </summary>
public static class SnowHttpClientNames
{
    /// <summary>
    /// 默认客户端: 由 <see cref="SnowHttpClientOptions"/> 配置基础地址、超时、UserAgent 与默认请求头
    /// </summary>
    public const string Default = "Snow";

    /// <summary>
    /// 按契约类型推导客户端名称, 规则见 <see cref="Normalize(string)"/>
    /// </summary>
    /// <typeparam name="T">客户端契约(接口)类型</typeparam>
    /// <returns>客户端名称, 如 <c>IHkoTrackingClient</c> 得到 <c>HkoTracking</c></returns>
    public static string For<T>() where T : class
    {
        return For(typeof(T));
    }

    /// <summary>
    /// 按契约类型推导客户端名称
    /// </summary>
    /// <param name="contractType">客户端契约(接口)类型</param>
    /// <returns>客户端名称</returns>
    public static string For(Type contractType)
    {
        ArgumentNullException.ThrowIfNull(contractType);

        // 泛型类型先截掉反射名里的 arity 后缀, 否则名字里会带反引号
        string name = contractType.Name;
        int arityIndex = name.IndexOf((char)96, StringComparison.Ordinal);
        if (arityIndex >= 0)
        {
            name = name[..arityIndex];
        }

        string normalized = Normalize(name);

        return normalized.Length == 0 ? name : normalized;
    }

    /// <summary>
    /// 规范化名称: 去掉前导 <c>I</c>(且其后紧跟大写字母)与尾部 <c>Client</c>
    /// <para><c>IHkoTrackingClient</c> 得到 <c>HkoTracking</c>; <c>IMDDB65127Client</c> 得到 <c>MDDB65127</c>; <c>IOMSClient</c> 得到 <c>OMS</c>。</para>
    /// </summary>
    /// <param name="name">类型短名</param>
    /// <returns>规范化后的名称, 可能为空串(如 <c>IClient</c>)</returns>
    public static string Normalize(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
        {
            name = name[1..];
        }

        const string suffix = "Client";
        if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
        {
            name = name[..^suffix.Length];
        }

        return name;
    }
}
