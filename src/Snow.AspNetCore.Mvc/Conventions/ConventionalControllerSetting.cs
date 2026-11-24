using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Snow.Reflection;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 单个程序集的约定控制器设置(对应 ABP 的 <c>ConventionalControllerSetting</c>)。
/// <para>Snow 未引入 Asp.Versioning, 所以 ABP 里与 API 版本化相关的 ApiVersions/MvcApiVersioningConfigurer 不在此提供。</para>
/// </summary>
public class ConventionalControllerSetting
{
    /// <summary>
    /// 要扫描的程序集
    /// </summary>
    public Assembly Assembly { get; }

    /// <summary>
    /// 扫描出的远程服务类型
    /// </summary>
    internal HashSet<Type> ControllerTypes { get; }

    /// <summary>
    /// 是否使用旧版 URL 风格; null 表示沿用 <see cref="SnowConventionalControllerOptions.UseV3UrlStyle"/>
    /// </summary>
    public bool? UseV3UrlStyle { get; set; }

    /// <summary>
    /// 根路径
    /// </summary>
    public string RootPath
    {
        get => _rootPath;
        set
        {
            Check.NotNull(value, nameof(value));
            _rootPath = value;
        }
    }
    private string _rootPath = default!;

    /// <summary>
    /// 远程服务名
    /// </summary>
    public string RemoteServiceName
    {
        get => _remoteServiceName;
        set
        {
            Check.NotNull(value, nameof(value));
            _remoteServiceName = value;
        }
    }
    private string _remoteServiceName = default!;

    /// <summary>
    /// 额外的类型过滤器
    /// </summary>
    public Func<Type, bool>? TypePredicate { get; set; }

    /// <summary>
    /// 要收集的应用服务类型, 默认 All
    /// </summary>
    public ApplicationServiceTypes ApplicationServiceTypes { get; set; } = ApplicationServiceTypes.All;

    /// <summary>
    /// 控制器模型回调
    /// </summary>
    public Action<ControllerModel>? ControllerModelConfigurer { get; set; }

    /// <summary>
    /// 自定义 URL 控制器名
    /// </summary>
    public Func<UrlControllerNameNormalizerContext, string>? UrlControllerNameNormalizer { get; set; }

    /// <summary>
    /// 自定义 URL action 名
    /// </summary>
    public Func<UrlActionNameNormalizerContext, string>? UrlActionNameNormalizer { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    public ConventionalControllerSetting(Assembly assembly, string rootPath, string remoteServiceName)
    {
        Assembly = Check.NotNull(assembly, nameof(assembly));
        RootPath = Check.NotNull(rootPath, nameof(rootPath));
        RemoteServiceName = Check.NotNull(remoteServiceName, nameof(remoteServiceName));

        ControllerTypes = new HashSet<Type>();
    }

    /// <summary>
    /// 扫描程序集, 收集符合条件的远程服务类型。
    /// </summary>
    public void Initialize()
    {
        var types = Assembly.GetTypes()
            .Where(IsRemoteService)
            .Where(IsPreferredApplicationServiceType)
            .WhereIf(TypePredicate != null, TypePredicate!);

        foreach (var type in types)
        {
            ControllerTypes.Add(type);
        }
    }

    /// <summary>
    /// 返回已收集的控制器类型快照。
    /// </summary>
    public IReadOnlyList<Type> GetControllerTypes()
    {
        return ControllerTypes.ToImmutableList();
    }

    private static bool IsRemoteService(Type type)
    {
        if (!type.IsPublic || type.IsAbstract || type.IsGenericType)
        {
            return false;
        }

        var remoteServiceAttr = ReflectionHelper.GetSingleAttributeOrDefault<RemoteServiceAttribute>(type);
        if (remoteServiceAttr != null && !remoteServiceAttr.IsEnabledFor(type))
        {
            return false;
        }

        return typeof(IRemoteService).IsAssignableFrom(type);
    }

    private bool IsPreferredApplicationServiceType(Type type)
    {
        if (ApplicationServiceTypes == ApplicationServiceTypes.ApplicationServices)
        {
            return !IntegrationServiceAttribute.IsDefinedOrInherited(type);
        }

        if (ApplicationServiceTypes == ApplicationServiceTypes.IntegrationServices)
        {
            return IntegrationServiceAttribute.IsDefinedOrInherited(type);
        }

        return true;
    }
}