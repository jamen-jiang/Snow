using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;
using Snow.AspNetCore;
using Snow.DependencyInjection;
using Snow.Http;
using Snow.Reflection;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 约定路由公式的实现。
/// </summary>
[TransientService]
public class ConventionalRouteBuilder
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

    /// <summary>
    /// 计算一个 action 的路由模板。
    /// </summary>
    /// <param name="rootPath">根路径</param>
    /// <param name="controllerName">控制器名</param>
    /// <param name="action">当前 action 模型</param>
    /// <param name="httpMethod">HTTP 方法</param>
    public virtual string Build(string rootPath, string controllerName, ActionModel action, string httpMethod)
    {
        var apiRoutePrefix = GetApiRoutePrefix(action);
        var controllerNameInUrl = NormalizeUrlControllerName(controllerName);

        var url = $"{apiRoutePrefix}/{rootPath}/{NormalizeControllerNameCase(controllerNameInUrl)}";

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
                    url += "/{" + NormalizeIdPropertyNameCase(property) + "}";
                }
            }
        }

        var actionNameInUrl = NormalizeUrlActionName(action.ActionName, httpMethod);
        if (!actionNameInUrl.IsNullOrEmpty())
        {
            url += $"/{NormalizeActionNameCase(actionNameInUrl)}";

            var secondaryIds = action.Parameters
                .Where(p => p.ParameterName.EndsWith("Id", StringComparison.Ordinal)).ToList();
            if (secondaryIds.Count == 1 && !secondaryIds[0].Attributes.Any(x => x is OptionalAttribute))
            {
                url += $"/{{{NormalizeSecondaryIdNameCase(secondaryIds[0])}}}";
            }
        }

        return url;
    }

    /// <summary>
    /// 根据是否集成服务决定 API 前缀。
    /// </summary>
    /// <param name="actionModel">当前 action 模型</param>
    protected virtual string GetApiRoutePrefix(ActionModel actionModel)
    {
        if (IntegrationServiceAttribute.IsDefinedOrInherited(actionModel.Controller.ControllerType))
        {
            return SnowAspNetCoreConsts.DefaultIntegrationServiceApiPrefix;
        }

        return SnowAspNetCoreConsts.DefaultApiPrefix;
    }

    /// <summary>
    /// 去掉 HTTP 方法前缀与 Async 后缀。
    /// </summary>
    /// <param name="actionName">动作名称</param>
    /// <param name="httpMethod">HTTP 方法</param>
    protected virtual string NormalizeUrlActionName(string actionName, string httpMethod)
    {
        return HttpMethodHelper
            .RemoveHttpMethodPrefix(actionName, httpMethod)
            .RemovePostFix("Async");
    }

    /// <summary>
    /// 规范化 URL 中的控制器名。
    /// </summary>
    /// <param name="controllerName">控制器名</param>
    protected virtual string NormalizeUrlControllerName(string controllerName)
    {
        return controllerName.RemovePostFix(Options.IgnoredUrlSuffixesInControllerNames);
    }

    /// <summary>
    /// 控制器名大小写风格。
    /// </summary>
    /// <param name="controllerName">控制器名</param>
    protected virtual string NormalizeControllerNameCase(string controllerName)
    {
        return controllerName.ToKebabCase();
    }

    /// <summary>
    /// action 名大小写风格。
    /// </summary>
    /// <param name="actionName">action 名称</param>
    protected virtual string NormalizeActionNameCase(string actionName)
    {
        return actionName.ToKebabCase();
    }

    /// <summary>
    /// id 复杂对象的属性段名称。
    /// </summary>
    /// <param name="property">属性信息</param>
    protected virtual string NormalizeIdPropertyNameCase(PropertyInfo property)
    {
        return property.Name;
    }

    /// <summary>
    /// 次级 id 参数名。
    /// </summary>
    /// <param name="secondaryId">次级 id 参数</param>
    protected virtual string NormalizeSecondaryIdNameCase(ParameterModel secondaryId)
    {
        return secondaryId.ParameterName;
    }
}