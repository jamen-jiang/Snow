using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;
using Snow.AspNetCore;
using Snow.DependencyInjection;
using Snow.Http;
using Snow.Reflection;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// ABP 约定路由公式的实现(对应 ABP 的 <c>ConventionalRouteBuilder</c>)。
/// </summary>
[TransientService]
public class ConventionalRouteBuilder : IConventionalRouteBuilder
{
    /// <summary>
    /// 约定控制器选项
    /// </summary>
    protected SnowConventionalControllerOptions Options { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">选项</param>
    public ConventionalRouteBuilder(IOptions<SnowConventionalControllerOptions> options)
    {
        Options = options.Value;
    }

    /// <inheritdoc />
    /// <param name="rootPath">根路径</param>
    /// <param name="controllerName">控制器名</param>
    /// <param name="action">当前 action 模型</param>
    /// <param name="httpMethod">HTTP 方法</param>
    /// <param name="configuration">约定配置, 可为 null</param>
    public virtual string Build(string rootPath, string controllerName, ActionModel action, string httpMethod, ConventionalControllerSetting? configuration)
    {
        var apiRoutePrefix = GetApiRoutePrefix(action, configuration);
        var controllerNameInUrl = NormalizeUrlControllerName(rootPath, controllerName, action, httpMethod, configuration);

        var url = $"{apiRoutePrefix}/{rootPath}/{NormalizeControllerNameCase(controllerNameInUrl, configuration)}";

        var idParameterModel = action.Parameters.FirstOrDefault(p => p.ParameterName == "id");
        if (idParameterModel != null)
        {
            if (TypeHelper.IsPrimitiveExtended(idParameterModel.ParameterType, includeEnums: true)
                || TypeDescriptor.GetConverter(idParameterModel.ParameterType).CanConvertFrom(typeof(string)))
            {
                url += "/{id}";
            }
            else
            {
                var properties = idParameterModel.ParameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                foreach (var property in properties)
                {
                    url += "/{" + NormalizeIdPropertyNameCase(property, configuration) + "}";
                }
            }
        }

        var actionNameInUrl = NormalizeUrlActionName(rootPath, controllerName, action, httpMethod, configuration);
        if (!actionNameInUrl.IsNullOrEmpty())
        {
            url += $"/{NormalizeActionNameCase(actionNameInUrl, configuration)}";

            var secondaryIds = action.Parameters
                .Where(p => p.ParameterName.EndsWith("Id", StringComparison.Ordinal)).ToList();
            if (secondaryIds.Count == 1 && !secondaryIds[0].Attributes.Any(x => x is OptionalAttribute))
            {
                url += $"/{{{NormalizeSecondaryIdNameCase(secondaryIds[0], configuration)}}}";
            }
        }

        return url;
    }

    /// <summary>
    /// 根据是否集成服务决定 API 前缀。
    /// </summary>
    /// <param name="actionModel">当前 action 模型</param>
    /// <param name="configuration">约定配置, 可为 null</param>
    protected virtual string GetApiRoutePrefix(ActionModel actionModel, ConventionalControllerSetting? configuration)
    {
        if (IntegrationServiceAttribute.IsDefinedOrInherited(actionModel.Controller.ControllerType))
        {
            return SnowAspNetCoreConsts.DefaultIntegrationServiceApiPrefix;
        }

        return SnowAspNetCoreConsts.DefaultApiPrefix;
    }

    /// <summary>
    /// 去掉 HTTP 方法前缀与 Async 后缀, 必要时调用自定义命名器。
    /// </summary>
    /// <param name="rootPath">根路径</param>
    /// <param name="controllerName">控制器名</param>
    /// <param name="action">当前 action 模型</param>
    /// <param name="httpMethod">HTTP 方法</param>
    /// <param name="configuration">约定配置, 可为 null</param>
    protected virtual string NormalizeUrlActionName(string rootPath, string controllerName, ActionModel action, string httpMethod, ConventionalControllerSetting? configuration)
    {
        var actionNameInUrl = HttpMethodHelper
            .RemoveHttpMethodPrefix(action.ActionName, httpMethod)
            .RemovePostFix("Async");

        if (configuration?.UrlActionNameNormalizer == null)
        {
            return actionNameInUrl;
        }

        return configuration.UrlActionNameNormalizer(
            new UrlActionNameNormalizerContext(rootPath, controllerName, action, actionNameInUrl, httpMethod)
        );
    }

    /// <summary>
    /// 规范化 URL 中的控制器名, 必要时调用自定义命名器。
    /// </summary>
    /// <param name="rootPath">根路径</param>
    /// <param name="controllerName">控制器名</param>
    /// <param name="action">当前 action 模型</param>
    /// <param name="httpMethod">HTTP 方法</param>
    /// <param name="configuration">约定配置, 可为 null</param>
    protected virtual string NormalizeUrlControllerName(string rootPath, string controllerName, ActionModel action, string httpMethod, ConventionalControllerSetting? configuration)
    {
        if (configuration?.UrlControllerNameNormalizer == null)
        {
            return controllerName.RemovePostFix(Options.IgnoredUrlSuffixesInControllerNames);
        }

        return configuration.UrlControllerNameNormalizer(
            new UrlControllerNameNormalizerContext(rootPath, controllerName)
        );
    }

    /// <summary>
    /// 控制器名大小写风格。
    /// </summary>
    /// <param name="controllerName">控制器名</param>
    /// <param name="configuration">约定配置, 可为 null</param>
    protected virtual string NormalizeControllerNameCase(string controllerName, ConventionalControllerSetting? configuration)
    {
        return (configuration?.UseV3UrlStyle ?? Options.UseV3UrlStyle) ? controllerName.ToCamelCase() : controllerName.ToKebabCase();
    }

    /// <summary>
    /// action 名大小写风格。
    /// </summary>
    /// <param name="actionName">action 名称</param>
    /// <param name="configuration">约定配置, 可为 null</param>
    protected virtual string NormalizeActionNameCase(string actionName, ConventionalControllerSetting? configuration)
    {
        return (configuration?.UseV3UrlStyle ?? Options.UseV3UrlStyle) ? actionName.ToCamelCase() : actionName.ToKebabCase();
    }

    /// <summary>
    /// id 复杂对象的属性段名称。
    /// </summary>
    /// <param name="property">属性信息</param>
    /// <param name="configuration">约定配置, 可为 null</param>
    protected virtual string NormalizeIdPropertyNameCase(PropertyInfo property, ConventionalControllerSetting? configuration)
    {
        return property.Name;
    }

    /// <summary>
    /// 次级 id 参数名。
    /// </summary>
    /// <param name="secondaryId">次级 id 参数</param>
    /// <param name="configuration">约定配置, 可为 null</param>
    protected virtual string NormalizeSecondaryIdNameCase(ParameterModel secondaryId, ConventionalControllerSetting? configuration)
    {
        return secondaryId.ParameterName;
    }
}