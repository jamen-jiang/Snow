using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Snow.Aop;
using Snow.DependencyInjection;
using Snow.Security.Users;
using Snow.Timing;

namespace Snow.Auditing;

/// <summary>
/// 审计辅助器默认实现
/// <para>判断规则全部取自 <see cref="AuditingOptions"/>: 总开关、忽略类型/命名空间、匿名用户、GET 请求。</para>
/// </summary>
[TransientService]
public sealed class AuditingHelper : IAuditingHelper
{
    private readonly AuditingOptions _options;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditSerializer _serializer;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">审计配置</param>
    /// <param name="currentUser">当前用户</param>
    /// <param name="clock">时钟</param>
    /// <param name="httpContextAccessor">HTTP 上下文访问器</param>
    /// <param name="serializer">序列化器</param>
    public AuditingHelper(
        IOptions<AuditingOptions> options,
        ICurrentUser currentUser,
        IClock clock,
        IHttpContextAccessor httpContextAccessor,
        IAuditSerializer serializer)
    {
        _options = options.Value;
        _currentUser = currentUser;
        _clock = clock;
        _httpContextAccessor = httpContextAccessor;
        _serializer = serializer;
    }

    /// <inheritdoc />
    public bool ShouldSaveAudit(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (!_options.IsEnabled)
        {
            return false;
        }

        Type? declaringType = method.DeclaringType;
        if (declaringType is not null)
        {
            if (_options.IgnoredTypes.Contains(declaringType))
            {
                return false;
            }

            if (!_options.AlwaysLogFrameworkTypes && IsIgnoredNamespace(declaringType))
            {
                return false;
            }
        }

        if (!_options.IsEnabledForAnonymousUsers && !_currentUser.IsAuthenticated)
        {
            return false;
        }

        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null && !_options.IsEnabledForGetRequests && HttpMethods.IsGet(httpContext.Request.Method))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public AuditLogInfo CreateAuditLogInfo()
    {
        AuditLogInfo logInfo = new()
        {
            ApplicationName = _options.ApplicationName ?? Assembly.GetEntryAssembly()?.GetName().Name,
            ExecutionTime = _clock.Now,
        };

        if (_currentUser.IsAuthenticated)
        {
            logInfo.UserId = _currentUser.Id;
            logInfo.UserName = _currentUser.UserName ?? _currentUser.Name;
        }

        FillHttpContext(logInfo);

        return logInfo;
    }

    /// <inheritdoc />
    public AuditLogActionInfo CreateAuditLogActionInfo(AopMethodInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        return new AuditLogActionInfo
        {
            ServiceName = invocation.Method.DeclaringType?.FullName,
            MethodName = invocation.Method.Name,
            Parameters = SerializeParameters(invocation),
            ExecutionTime = _clock.Now,
        };
    }

    /// <inheritdoc />
    public void FillHttpContext(AuditLogInfo logInfo)
    {
        ArgumentNullException.ThrowIfNull(logInfo);

        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        HttpRequest request = httpContext.Request;
        logInfo.HttpMethod = request.Method;
        logInfo.Url = request.Path.HasValue ? request.Path.Value : null;
        logInfo.ClientIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        logInfo.UserAgent = request.Headers.UserAgent.ToString();
        logInfo.HttpStatusCode = httpContext.Response.StatusCode;
    }

    private bool IsIgnoredNamespace(Type type)
    {
        string? @namespace = type.Namespace;
        if (string.IsNullOrEmpty(@namespace))
        {
            return false;
        }

        foreach (string prefix in _options.IgnoredNamespaces)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                continue;
            }

            if (@namespace.Equals(prefix, StringComparison.Ordinal) || @namespace.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private string? SerializeParameters(AopMethodInvocation invocation)
    {
        ParameterInfo[] parameters = invocation.Method.GetParameters();
        if (parameters.Length == 0)
        {
            return null;
        }

        Dictionary<string, object?> arguments = new(parameters.Length, StringComparer.Ordinal);
        for (int index = 0; index < parameters.Length && index < invocation.Arguments.Length; index++)
        {
            object? value = invocation.Arguments[index];

            // 取消令牌没有审计价值, 序列化出来也是一串内部字段
            if (value is CancellationToken)
            {
                continue;
            }

            arguments[parameters[index].Name ?? "arg" + index] = value;
        }

        return arguments.Count == 0 ? null : _serializer.Serialize(arguments);
    }
}
