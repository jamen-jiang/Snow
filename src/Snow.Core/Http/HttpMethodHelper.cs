using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Snow.Exceptions;

namespace Snow.Http;

/// <summary>
/// HTTP 动词辅助方法(对应 ABP 的 <c>HttpMethodHelper</c>): 按方法名前缀推断动词, 并按动词去掉方法名前缀。
/// </summary>
public static class HttpMethodHelper
{
    /// <summary>
    /// GET
    /// </summary>
    public const string Get = "GET";
    /// <summary>
    /// POST
    /// </summary>
    public const string Post = "POST";
    /// <summary>
    /// PUT
    /// </summary>
    public const string Put = "PUT";
    /// <summary>
    /// DELETE
    /// </summary>
    public const string Delete = "DELETE";
    /// <summary>
    /// PATCH
    /// </summary>
    public const string Patch = "PATCH";
    /// <summary>
    /// HEAD
    /// </summary>
    public const string Head = "HEAD";
    /// <summary>
    /// OPTIONS
    /// </summary>
    public const string Options = "OPTIONS";
    /// <summary>
    /// TRACE
    /// </summary>
    public const string Trace = "TRACE";
    /// <summary>
    /// QUERY
    /// </summary>
    public const string Query = "QUERY";

    /// <summary>
    /// 默认动词
    /// </summary>
    public const string DefaultHttpVerb = Post;

    /// <summary>
    /// 方法名前缀到 HTTP 动词的映射
    /// </summary>
    public static Dictionary<string, string[]> ConventionalPrefixes { get; set; } = new()
    {
        { Get, new[] { "GetList", "GetAll", "Get" } },
        { Put, new[] { "Put", "Update" } },
        { Delete, new[] { "Delete", "Remove" } },
        { Post, new[] { "Create", "Add", "Insert", "Post" } },
        { Patch, new[] { "Patch" } }
    };

    /// <summary>
    /// 按方法名前缀推断 HTTP 动词, 没有命中时返回 POST。
    /// </summary>
    /// <param name="methodName">方法名</param>
    public static string GetConventionalVerbForMethodName(string methodName)
    {
        foreach (var conventionalPrefix in ConventionalPrefixes)
        {
            if (conventionalPrefix.Value.Any(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return conventionalPrefix.Key;
            }
        }

        return DefaultHttpVerb;
    }

    /// <summary>
    /// 按 HTTP 动词去掉方法名开头对应的前缀。
    /// </summary>
    /// <param name="methodName">方法名</param>
    /// <param name="httpMethod">HTTP 方法</param>
    public static string RemoveHttpMethodPrefix(string methodName, string httpMethod)
    {
        Check.NotNull(methodName, nameof(methodName));
        Check.NotNull(httpMethod, nameof(httpMethod));

        var prefixes = ConventionalPrefixes.GetOrDefault(httpMethod);
        if (prefixes.IsNullOrEmpty())
        {
            return methodName;
        }

        return methodName.RemovePreFix(prefixes!);
    }

    /// <summary>
    /// 把字符串形式的 HTTP 动词转成 <see cref="HttpMethod"/>。
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static HttpMethod ConvertToHttpMethod(string? httpMethod)
    {
        switch (httpMethod?.ToUpperInvariant())
        {
            case Get: return HttpMethod.Get;
            case Post: return HttpMethod.Post;
            case Put: return HttpMethod.Put;
            case Delete: return HttpMethod.Delete;
            case Options: return HttpMethod.Options;
            case Trace: return HttpMethod.Trace;
            case Head: return HttpMethod.Head;
            case Patch: return new HttpMethod(Patch);
            case Query: return new HttpMethod(Query);
            default: throw new SnowException("Unknown HTTP METHOD: " + httpMethod);
        }
    }

    /// <summary>
    /// 是否为 GET
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsGet(string? httpMethod) => string.Equals(httpMethod, Get, StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 是否为 POST
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsPost(string? httpMethod) => string.Equals(httpMethod, Post, StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 是否为 PUT
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsPut(string? httpMethod) => string.Equals(httpMethod, Put, StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 是否为 DELETE
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsDelete(string? httpMethod) => string.Equals(httpMethod, Delete, StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 是否为 PATCH
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsPatch(string? httpMethod) => string.Equals(httpMethod, Patch, StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 是否为 HEAD
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsHead(string? httpMethod) => string.Equals(httpMethod, Head, StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 是否为 OPTIONS
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsOptions(string? httpMethod) => string.Equals(httpMethod, Options, StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 是否为 TRACE
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsTrace(string? httpMethod) => string.Equals(httpMethod, Trace, StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// 是否为 QUERY
    /// </summary>
    /// <param name="httpMethod">HTTP 方法</param>
    public static bool IsQuery(string? httpMethod) => string.Equals(httpMethod, Query, StringComparison.OrdinalIgnoreCase);
}