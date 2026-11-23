using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 为自动发现的应用服务计算路由与 HTTP 方法(对应 ABP 的 AbpServiceConvention)
/// <para>约定: 根路径 + 控制器名(去 AppService 后缀) + 可选 {id}; 动词按方法名前缀判定。</para>
/// </summary>
public sealed class SnowApplicationServiceConvention : IApplicationModelConvention
{
    private static readonly string[] ControllerSuffixes = ["ApplicationService", "AppService", "Service"];

    private readonly SnowMvcOptions _options;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">MVC 选项</param>
    public SnowApplicationServiceConvention(SnowMvcOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public void Apply(ApplicationModel application)
    {
        ArgumentNullException.ThrowIfNull(application);

        foreach (ControllerModel controller in application.Controllers)
        {
            if (!SnowApplicationServiceHelper.IsApplicationService(controller.ControllerType.AsType()))
            {
                continue;
            }

            ApplyController(controller);
        }
    }

    private void ApplyController(ControllerModel controller)
    {
        string controllerName = GetControllerName(controller.ControllerType.AsType());
        controller.ControllerName = controllerName;
        controller.ApiExplorer.IsVisible = true;
        controller.ApiExplorer.GroupName ??= controllerName;

        string controllerPath = _options.RootPath.TrimEnd('/') + "/" + ToKebabCase(controllerName);

        foreach (ActionModel action in controller.Actions)
        {
            ApplyAction(action, controllerPath);
        }
    }

    private static void ApplyAction(ActionModel action, string controllerPath)
    {
        string methodName = action.ActionMethod.Name;
        action.ActionName = TrimAsyncSuffix(methodName);
        action.ApiExplorer.IsVisible = true;

        ParameterModel? idParameter = action.Parameters
            .FirstOrDefault(parameter => string.Equals(parameter.ParameterName, "id", StringComparison.OrdinalIgnoreCase)
                                         && IsSimpleType(parameter.ParameterInfo.ParameterType));

        (string verb, string template) = Resolve(methodName, action, idParameter, controllerPath);

        action.Selectors.Clear();
        SelectorModel selector = new()
        {
            AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(template)),
        };
        selector.ActionConstraints.Add(new HttpMethodActionConstraint([verb]));
        action.Selectors.Add(selector);

        ConfigureParameters(action);
    }

    private static (string Verb, string Template) Resolve(string methodName, ActionModel action, ParameterModel? idParameter, string controllerPath)
    {
        string name = TrimAsyncSuffix(methodName);
        bool singleSimpleIdOnly = idParameter is not null
            && action.Parameters.Count(parameter => parameter.ParameterInfo.ParameterType != typeof(CancellationToken)) == 1;

        if (StartsWithAny(name, "GetList", "GetAll", "GetPaged", "List"))
        {
            return ("GET", controllerPath);
        }

        if (StartsWithAny(name, "Get", "Find", "Load"))
        {
            return idParameter is not null && singleSimpleIdOnly
                ? ("GET", controllerPath + "/{id}")
                : ("GET", controllerPath + "/" + ToKebabCase(name));
        }

        if (StartsWithAny(name, "Create", "Add", "Insert", "Post"))
        {
            return ("POST", controllerPath);
        }

        if (StartsWithAny(name, "Update", "Edit", "Modify", "Put"))
        {
            return idParameter is not null
                ? ("PUT", controllerPath + "/{id}")
                : ("PUT", controllerPath + "/" + ToKebabCase(name));
        }

        if (StartsWithAny(name, "Patch"))
        {
            return idParameter is not null
                ? ("PATCH", controllerPath + "/{id}")
                : ("PATCH", controllerPath + "/" + ToKebabCase(name));
        }

        if (StartsWithAny(name, "Delete", "Remove", "RemoveBy"))
        {
            return idParameter is not null
                ? ("DELETE", controllerPath + "/{id}")
                : ("DELETE", controllerPath + "/" + ToKebabCase(name));
        }

        return ("POST", controllerPath + "/" + ToKebabCase(name));
    }

    private static void ConfigureParameters(ActionModel action)
    {
        foreach (ParameterModel parameter in action.Parameters)
        {
            if (parameter.BindingInfo is not null)
            {
                continue;
            }

            Type parameterType = parameter.ParameterInfo.ParameterType;

            // 取消令牌由框架注入, 不参与绑定
            if (parameterType == typeof(CancellationToken))
            {
                continue;
            }

            if (string.Equals(parameter.ParameterName, "id", StringComparison.OrdinalIgnoreCase) && IsSimpleType(parameterType))
            {
                parameter.BindingInfo = BindingInfo.GetBindingInfo([new FromRouteAttribute()]);
                continue;
            }

            // 简单类型走查询串/路由, MVC 默认即可; 复杂类型必须显式声明来自请求体
            if (!IsSimpleType(parameterType))
            {
                parameter.BindingInfo = BindingInfo.GetBindingInfo([new FromBodyAttribute()]);
            }
        }
    }

    private static string GetControllerName(Type type)
    {
        string name = type.Name;
        foreach (string suffix in ControllerSuffixes)
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name[..^suffix.Length];
                break;
            }
        }

        return name;
    }

    private static string TrimAsyncSuffix(string name)
        => name.EndsWith("Async", StringComparison.Ordinal) && name.Length > 5 ? name[..^5] : name;

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        System.Text.StringBuilder builder = new(name.Length + 8);
        for (int index = 0; index < name.Length; index++)
        {
            char current = name[index];
            if (char.IsUpper(current))
            {
                if (index > 0 && name[index - 1] != '-' && !char.IsUpper(name[index - 1]))
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }

    private static bool StartsWithAny(string value, params string[] prefixes)
        => prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));

    private static bool IsSimpleType(Type type)
    {
        Type target = Nullable.GetUnderlyingType(type) ?? type;
        return target.IsPrimitive
               || target.IsEnum
               || target == typeof(string)
               || target == typeof(decimal)
               || target == typeof(Guid)
               || target == typeof(DateTime)
               || target == typeof(DateTimeOffset)
               || target == typeof(TimeSpan)
               || target == typeof(DateOnly)
               || target == typeof(TimeOnly)
               || target == typeof(Uri);
    }
}
