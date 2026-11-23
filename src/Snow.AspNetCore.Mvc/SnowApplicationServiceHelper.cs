using System.Reflection;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 应用服务识别规则
/// <para>消费方程序集里、类名以 <c>ApplicationService</c> / <c>AppService</c> / <c>Service</c> 结尾的具体类视为应用服务,
/// 由特性提供器登记成 Controller。框架自己的程序集(<c>Snow.*</c>)不参与, 免得把 <c>TokenService</c> 这类普通服务暴露成 API。</para>
/// </summary>
internal static class SnowApplicationServiceHelper
{
    private static readonly string[] Suffixes = ["ApplicationService", "AppService", "Service"];

    /// <summary>
    /// 判断一个类型是否算应用服务
    /// </summary>
    /// <param name="type">待判断的类型</param>
    /// <returns>是应用服务时返回 <see langword="true"/></returns>
    internal static bool IsApplicationService(Type type)
    {
        if (!type.IsClass || type.IsAbstract || !type.IsVisible)
        {
            return false;
        }

        string? assemblyName = type.Assembly.GetName().Name;
        if (assemblyName is not null && assemblyName.StartsWith("Snow.", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (string suffix in Suffixes)
        {
            if (type.Name.Length > suffix.Length && type.Name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
