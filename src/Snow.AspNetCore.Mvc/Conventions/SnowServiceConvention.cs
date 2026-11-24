using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Snow.AspNetCore.Controllers;
using Snow.Http;
using Snow.Http.Modeling;
using Snow.DependencyInjection;
using Snow.Reflection;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 实现 ABP 的约定路由主逻辑(对应 ABP 的 <c>AbpServiceConvention</c>)。
/// <para>只对真正的 Controller 生效: 实现 <see cref="IRemoteService"/> 或标了 <see cref="RemoteServiceAttribute"/> 的控制器才会被套用约定路由。</para>
/// </summary>
[TransientService]
public class SnowServiceConvention : ISnowServiceConvention
{
    /// <summary>
    /// 约定控制器选项
    /// </summary>
    protected SnowAspNetCoreMvcOptions Options { get; }

    /// <summary>
    /// 约定路由构造器
    /// </summary>
    protected IConventionalRouteBuilder ConventionalRouteBuilder { get; }

    /// <summary>
    /// 日志
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="options">选项</param>
    /// <param name="conventionalRouteBuilder">约定路由构造器</param>
    /// <param name="logger">日志器</param>
    public SnowServiceConvention(
        IOptions<SnowAspNetCoreMvcOptions> options,
        IConventionalRouteBuilder conventionalRouteBuilder,
        ILogger<SnowServiceConvention> logger)
    {
        Options = options.Value;
        ConventionalRouteBuilder = conventionalRouteBuilder;
        Logger = logger ?? NullLogger<SnowServiceConvention>.Instance;
    }

    /// <inheritdoc />
    /// <param name="application">应用模型</param>
    public void Apply(ApplicationModel application)
    {
        ApplyForControllers(application);
    }

    /// <summary>
    /// 对所有控制器应用约定。
    /// </summary>
    /// <param name="application">应用模型</param>
    protected virtual void ApplyForControllers(ApplicationModel application)
    {
        RemoveDuplicateControllers(application);
        RemoveIntegrationControllersIfNotExposed(application);

        foreach (var controller in application.Controllers)
        {
            var controllerSetting = GetControllerSettingOrNull(controller.ControllerType);

            if (ImplementsRemoteServiceInterface(controller.ControllerType))
            {
                controller.ControllerName = controller.ControllerName.RemovePostFix(SnowApplicationServicePostfixes.CommonPostfixes);
                controllerSetting?.ControllerModelConfigurer?.Invoke(controller);
                ConfigureRemoteService(controller, controllerSetting);
            }
            else
            {
                var remoteServiceAttr = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(controller.ControllerType);
                if (remoteServiceAttr != null && remoteServiceAttr.IsEnabledFor(controller.ControllerType))
                {
                    ConfigureRemoteService(controller, controllerSetting);
                }
            }
        }
    }

    /// <summary>
    /// 按 ABP 规则移除重复/被替换的控制器。
    /// </summary>
    /// <param name="application">应用模型</param>
    protected virtual void RemoveDuplicateControllers(ApplicationModel application)
    {
        var controllerModelsToRemove = new List<ControllerModel>();

        foreach (var controllerModel in application.Controllers)
        {
            if (Options.ControllersToRemove.Any(type => controllerModel.ControllerType.IsAssignableFrom(type)))
            {
                controllerModelsToRemove.Add(controllerModel);
                continue;
            }

            var replaceControllersAttribute = ReflectionHelper.GetSingleAttributeOrDefault<ReplaceControllersAttribute>(controllerModel.ControllerType);
            if (replaceControllersAttribute != null &&
                replaceControllersAttribute.ControllerTypes.Any(type => controllerModel.ControllerType.IsAssignableFrom(type)))
            {
                controllerModelsToRemove.Add(controllerModel);
                continue;
            }

            var exposeServicesAttribute = ReflectionHelper.GetSingleAttributeOrDefault<ExposeServicesAttribute>(controllerModel.ControllerType);
            if (exposeServicesAttribute == null)
            {
                continue;
            }

            if (Options.IgnoredControllersOnModelExclusion.Contains(controllerModel.ControllerType))
            {
                continue;
            }

            if (exposeServicesAttribute.IncludeSelf)
            {
                controllerModelsToRemove.AddRange(
                    application.Controllers.Where(x =>
                        x != controllerModel &&
                        exposeServicesAttribute.ServiceTypes.Any(t => t.IsAssignableFrom(x.ControllerType)))
                );
                continue;
            }

            if (controllerModel.ControllerType
                .GetBaseClasses(typeof(Controller), includeObject: false)
                .Where(x => !x.IsAbstract)
                .Any(t => exposeServicesAttribute.ServiceTypes.Contains(t)))
            {
                controllerModelsToRemove.Add(controllerModel);
            }
        }

        application.Controllers.RemoveAll(controllerModelsToRemove);
    }

    /// <summary>
    /// 根据选项移除不应暴露的集成服务/客户端代理服务控制器。
    /// </summary>
    /// <param name="application">应用模型</param>
    protected virtual void RemoveIntegrationControllersIfNotExposed(ApplicationModel application)
    {
        if (!Options.ExposeIntegrationServices)
        {
            application.Controllers.RemoveAll(
                controller => IntegrationServiceAttribute.IsDefinedOrInherited(controller.ControllerType)
            );
        }

        if (!Options.ExposeClientProxyServices)
        {
            application.Controllers.RemoveAll(IsClientProxyService);
        }
    }

    /// <summary>
    /// Snow 无客户端代理基类, 恒返回 false(保留 ABP 方法结构)。
    /// </summary>
    /// <param name="controller">控制器模型</param>
    protected virtual bool IsClientProxyService(ControllerModel controller)
    {
        return false;
    }

    /// <summary>
    /// 配置控制器的 API Explorer、选择器和参数。
    /// </summary>
    /// <param name="controller">控制器模型</param>
    /// <param name="controllerSetting">约定配置, 可为 null</param>
    protected virtual void ConfigureRemoteService(ControllerModel controller, ConventionalControllerSetting? controllerSetting)
    {
        ConfigureApiExplorer(controller);
        ConfigureSelector(controller, controllerSetting);
        ConfigureParameters(controller);
    }

    /// <summary>
    /// 给复杂参数加请求体绑定。
    /// </summary>
    /// <param name="controller">控制器模型</param>
    protected virtual void ConfigureParameters(ControllerModel controller)
    {
        foreach (var action in controller.Actions)
        {
            foreach (var parameter in action.Parameters)
            {
                if (parameter.BindingInfo != null)
                {
                    continue;
                }

                if (TypeHelper.IsPrimitiveExtended(parameter.ParameterType, includeEnums: true))
                {
                    continue;
                }

                if (!CanUseFormBodyBinding(action, parameter))
                {
                    continue;
                }

                parameter.BindingInfo = BindingInfo.GetBindingInfo(new[] { new FromBodyAttribute() });
            }
        }
    }

    /// <summary>
    /// 判断某参数能否使用请求体绑定(与 ABP 一致)。
    /// </summary>
    /// <param name="action">当前 action 模型</param>
    /// <param name="parameter">参数模型</param>
    protected virtual bool CanUseFormBodyBinding(ActionModel action, ParameterModel parameter)
    {
        if (parameter.ParameterName == "id")
        {
            return false;
        }

        if (Options.ConventionalControllers.FormBodyBindingIgnoredTypes.Any(type => type.IsAssignableFrom(parameter.ParameterType)))
        {
            return false;
        }

        foreach (var selector in action.Selectors)
        {
            foreach (var constraint in selector.ActionConstraints)
            {
                if (constraint is not HttpMethodActionConstraint httpMethodActionConstraint)
                {
                    continue;
                }

                if (httpMethodActionConstraint.HttpMethods.All(verb =>
                        HttpMethodHelper.Get == verb ||
                        HttpMethodHelper.Delete == verb ||
                        HttpMethodHelper.Trace == verb ||
                        HttpMethodHelper.Head == verb))
                {
                    return false;
                }

                return true;
            }
        }

        return true;
    }

    /// <summary>
    /// 配置 API Explorer 可见性与分组名。
    /// </summary>
    protected virtual void ConfigureApiExplorer(ControllerModel controller)
    {
        if (Options.ChangeControllerModelApiExplorerGroupName && controller.ApiExplorer.GroupName.IsNullOrEmpty())
        {
            controller.ApiExplorer.GroupName = controller.ControllerName;
        }

        controller.ApiExplorer.IsVisible ??= IsVisibleRemoteService(controller.ControllerType);

        foreach (var action in controller.Actions)
        {
            if (action.ApiExplorer.IsVisible != null)
            {
                continue;
            }
            action.ApiExplorer.IsVisible = IsVisibleRemoteServiceMethod(action.ActionMethod);
        }
    }

    /// <summary>
    /// 为控制器/动作生成选择器与路由。
    /// </summary>
    protected virtual void ConfigureSelector(ControllerModel controller, ConventionalControllerSetting? controllerSetting)
    {
        RemoveEmptySelectors(controller.Selectors);

        var remoteServiceAttr = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(controller.ControllerType);
        if (remoteServiceAttr != null && !remoteServiceAttr.IsEnabledFor(controller.ControllerType))
        {
            return;
        }

        if (controller.Selectors.Any(selector => selector.AttributeRouteModel != null))
        {
            return;
        }

        var rootPath = GetRootPathOrDefault(controller.ControllerType);
        foreach (var action in controller.Actions)
        {
            ConfigureSelector(rootPath, controller.ControllerName, action, controllerSetting);
        }
    }

    /// <summary>
    /// 为单个动作配置选择器。
    /// </summary>
protected virtual void ConfigureSelector(string rootPath, string controllerName, ActionModel action, ConventionalControllerSetting? controllerSetting)
    {
        RemoveEmptySelectors(action.Selectors);

        var remoteServiceAttr = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(action.ActionMethod);
        if (remoteServiceAttr != null && !remoteServiceAttr.IsEnabledFor(action.ActionMethod))
        {
            return;
        }

        if (!action.Selectors.Any())
        {
            AddSnowServiceSelector(rootPath, controllerName, action, controllerSetting);
        }
        else
        {
            NormalizeSelectorRoutes(rootPath, controllerName, action, controllerSetting);
        }
    }

    /// <summary>
    /// 添加约定选择器。
    /// </summary>
    protected virtual void AddSnowServiceSelector(string rootPath, string controllerName, ActionModel action, ConventionalControllerSetting? controllerSetting)
    {
        var httpMethod = SelectHttpMethod(action, controllerSetting);

        var selector = new SelectorModel
        {
            AttributeRouteModel = CreateSnowServiceAttributeRouteModel(rootPath, controllerName, action, httpMethod, controllerSetting),
            ActionConstraints =
            {
                new HttpMethodActionConstraint(new[] { httpMethod })
            }
        };

        action.Selectors.Add(selector);
    }

    /// <summary>
    /// 规范已有选择器的路由。
    /// </summary>
    protected virtual void NormalizeSelectorRoutes(string rootPath, string controllerName, ActionModel action, ConventionalControllerSetting? controllerSetting)
    {
        foreach (var selector in action.Selectors)
        {
            var httpMethod = selector.ActionConstraints
                .OfType<HttpMethodActionConstraint>()
                .SelectMany(c => c.HttpMethods)
                .FirstOrDefault() ?? SelectHttpMethod(action, controllerSetting);

            selector.AttributeRouteModel ??= CreateSnowServiceAttributeRouteModel(rootPath, controllerName, action, httpMethod, controllerSetting);

            if (!selector.ActionConstraints.OfType<HttpMethodActionConstraint>().Any())
            {
                selector.ActionConstraints.Add(new HttpMethodActionConstraint(new[] { httpMethod }));
            }
        }
    }

    /// <summary>
    /// 根据动作名推断 HTTP 动词。
    /// </summary>
    protected virtual string SelectHttpMethod(ActionModel action, ConventionalControllerSetting? controllerSetting)
    {
        return HttpMethodHelper.GetConventionalVerbForMethodName(action.ActionName);
    }

    /// <summary>
    /// 创建约定路由模型。
    /// </summary>
    protected virtual AttributeRouteModel CreateSnowServiceAttributeRouteModel(
        string rootPath,
        string controllerName,
        ActionModel action,
        string httpMethod,
        ConventionalControllerSetting? controllerSetting)
    {
        return new AttributeRouteModel(
            new RouteAttribute(
                ConventionalRouteBuilder.Build(rootPath, controllerName, action, httpMethod, controllerSetting)
            )
        );
    }

    /// <summary>
    /// 移除空选择器。
    /// </summary>
    protected virtual void RemoveEmptySelectors(IList<SelectorModel> selectors)
    {
        selectors
            .Where(IsEmptySelector)
            .ToList()
            .ForEach(s => selectors.Remove(s));
    }

    /// <summary>
    /// 判断是否为空选择器。
    /// </summary>
    protected virtual bool IsEmptySelector(SelectorModel selector)
    {
        return selector.AttributeRouteModel == null
               && selector.ActionConstraints.IsNullOrEmpty()
               && selector.EndpointMetadata.IsNullOrEmpty();
    }

    /// <summary>
    /// 取得控制器对应的约定设置。
    /// </summary>
    protected virtual ConventionalControllerSetting? GetControllerSettingOrNull(Type controllerType)
    {
        return Options.ConventionalControllers.ConventionalControllerSettings.GetSettingOrNull(controllerType);
    }

    /// <summary>
    /// 决定控制器根路径。
    /// </summary>
    protected virtual string GetRootPathOrDefault(Type controllerType)
    {
        var controllerSetting = GetControllerSettingOrNull(controllerType);
        if (controllerSetting?.RootPath != null)
        {
            return controllerSetting.RootPath;
        }

        var areaAttribute = controllerType.GetCustomAttribute<AreaAttribute>();
        if (areaAttribute?.RouteValue != null)
        {
            return areaAttribute.RouteValue;
        }

        return ModuleApiDescriptionModel.DefaultRootPath;
    }

    /// <summary>
    /// 是否实现 <see cref="IRemoteService"/>。
    /// </summary>
    protected virtual bool ImplementsRemoteServiceInterface(Type controllerType)
    {
        return typeof(IRemoteService).IsAssignableFrom(controllerType);
    }

    /// <summary>
    /// 控制器是否对 API Explorer 可见。
    /// </summary>
    protected virtual bool IsVisibleRemoteService(Type controllerType)
    {
        if (!IsGlobalFeatureEnabled(controllerType))
        {
            return false;
        }

        var remoteServiceAttr = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(controllerType);
        if (remoteServiceAttr == null)
        {
            return true;
        }

        return remoteServiceAttr.IsEnabledFor(controllerType) && remoteServiceAttr.IsMetadataEnabledFor(controllerType);
    }

    /// <summary>
    /// 动作是否对 API Explorer 可见; null 表示不覆盖默认。
    /// </summary>
    protected virtual bool? IsVisibleRemoteServiceMethod(MethodInfo method)
    {
        var remoteServiceAttr = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(method);
        if (remoteServiceAttr == null)
        {
            return null;
        }

        return remoteServiceAttr.IsEnabledFor(method) && remoteServiceAttr.IsMetadataEnabledFor(method);
    }

    /// <summary>
    /// Snow 未实现全局特性系统, 恒返回 true(保留 ABP 方法结构)。
    /// </summary>
    protected virtual bool IsGlobalFeatureEnabled(Type controllerType)
    {
        return true;
    }
}

/// <summary>
/// 应用服务类名的常用后缀(对应 ABP ApplicationService.CommonPostfixes)。
/// </summary>
public static class SnowApplicationServicePostfixes
{
    /// <summary>
    /// 常用后缀
    /// </summary>
    public static string[] CommonPostfixes => new[] { "AppService", "ApplicationService", "Service" };
}