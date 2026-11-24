using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Snow.Messaging.Registration;

/// <summary>
/// 扫描程序集,把命令/查询处理器、通知处理器、管道行为、前置/后置处理器按约定注册到 DI 容器
/// </summary>
public static class ServiceRegistrar
{
    /// <summary>
    /// 泛型请求允许的类型参数个数上限
    /// </summary>
    private const int MaxGenericTypeParameters = 10;

    /// <summary>
    /// 用于闭合泛型类型参数约束的类型个数上限
    /// </summary>
    private const int MaxTypesClosing = 100;

    /// <summary>
    /// 允许尝试注册的泛型请求处理器数量上限
    /// </summary>
    private const int MaxGenericTypeRegistrations = 125000;

    /// <summary>
    /// 泛型处理器注册过程的超时时间(毫秒)
    /// </summary>
    private const int RegistrationTimeout = 15000;

    /// <summary>
    /// 带超时地执行全部注册
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">待扫描的程序集</param>
    public static void AddMessagingClassesWithTimeout(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        // 超时设为 0 或负数表示禁用超时;否则 CancellationTokenSource 传入 0 会立刻取消
        using(var cts = new CancellationTokenSource(RegistrationTimeout > 0 ? RegistrationTimeout : Timeout.Infinite))
        {
            try
            {
                AddMessagingClasses(services, assemblies, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("The generic handler registration process timed out.");
            }
        }
    }

    /// <summary>
    /// 注册主流程:连接处理器、注册开放式泛型服务、注册前置/后置处理器与管道行为
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">待扫描的程序集</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static void AddMessagingClasses(IServiceCollection services, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {   

        var assembliesToScan = assemblies.Distinct().ToArray();

        ConnectImplementationsToTypesClosing(typeof(ICommandHandler<,>), services, assembliesToScan, false, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(ICommandHandler<>), services, assembliesToScan, false, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IQueryHandler<,>), services, assembliesToScan, false, cancellationToken);
    }

    /// <summary>
    /// 把实现了指定开放接口的类型登记到容器
    /// </summary>
    /// <param name="openRequestInterface">开放泛型接口</param>
    /// <param name="services">服务集合</param>
    /// <param name="assembliesToScan">要扫描的程序集</param>
    /// <param name="addIfAlreadyExists">已存在时是否重复添加</param>
    /// <param name="cancellationToken">取消令牌</param>
    private static void ConnectImplementationsToTypesClosing(Type openRequestInterface,
        IServiceCollection services,
        IEnumerable<Assembly> assembliesToScan,
        bool addIfAlreadyExists,
        CancellationToken cancellationToken = default)
    {
        var concretions = new List<Type>();
        var interfaces = new List<Type>();
        var genericConcretions = new List<Type>();
        var genericInterfaces = new List<Type>();

        var types = assembliesToScan
            .SelectMany(a => a.GetLoadableDefinedTypes())
            .Where(t => !t.ContainsGenericParameters)
            .Where(t => t.IsConcrete() && t.FindInterfacesThatClose(openRequestInterface).Any())
            .ToList();

        foreach (var type in types)
        {
            var interfaceTypes = type.FindInterfacesThatClose(openRequestInterface).ToArray();

            if (!type.IsOpenGeneric())
            {
                concretions.Add(type);

                foreach (var interfaceType in interfaceTypes)
                {
                    interfaces.Fill(interfaceType);
                }
            }
            else
            {
                genericConcretions.Add(type);
                foreach (var interfaceType in interfaceTypes)
                {
                    genericInterfaces.Fill(interfaceType);
                }
            }
        }

        foreach (var @interface in interfaces)
        {
            var exactMatches = concretions.Where(x => x.CanBeCastTo(@interface)).ToList();
            if (addIfAlreadyExists)
            {
                foreach (var type in exactMatches)
                {
                    services.AddTransient(@interface, type);
                }
            }
            else
            {
                if (exactMatches.Count > 1)
                {
                    exactMatches.RemoveAll(m => !IsMatchingWithInterface(m, @interface));
                }

                foreach (var type in exactMatches)
                {
                    services.TryAddTransient(@interface, type);
                }
            }

            if (!@interface.IsOpenGeneric())
            {
                AddConcretionsThatCouldBeClosed(@interface, concretions, services);
            }
        }

        foreach (var @interface in genericInterfaces)
        {
            var exactMatches = genericConcretions.Where(x => x.CanBeCastTo(@interface)).ToList();
            AddAllConcretionsThatClose(@interface, exactMatches, services, assembliesToScan, cancellationToken);
        }
    }

    /// <summary>
    /// 判断处理器类型是否与给定的开放接口匹配
    /// </summary>
    private static bool IsMatchingWithInterface(Type? handlerType, Type handlerInterface)
    {
        if (handlerType == null || handlerInterface == null)
        {
            return false;
        }

        if (handlerType.IsInterface)
        {
            if (handlerType.GenericTypeArguments.SequenceEqual(handlerInterface.GenericTypeArguments))
            {
                return true;
            }
        }
        else
        {
            return IsMatchingWithInterface(handlerType.GetInterface(handlerInterface.Name), handlerInterface);
        }

        return false;
    }

    /// <summary>
    /// 把能够闭合到指定接口的具体类型登记进容器
    /// </summary>
    private static void AddConcretionsThatCouldBeClosed(Type @interface, List<Type> concretions, IServiceCollection services)
    {
        foreach (var type in concretions
                     .Where(x => x.IsOpenGeneric() && x.CouldCloseTo(@interface)))
        {
            try
            {
                services.TryAddTransient(@interface, type.MakeGenericType(@interface.GenericTypeArguments));
            }
            catch (Exception)
            {
            }
        }
    }

    /// <summary>
    /// 计算某个具体请求对应的服务类型与实现类型
    /// </summary>
    private static (Type Service, Type Implementation) GetConcreteRegistrationTypes(Type openRequestHandlerInterface, Type concreteGenericTRequest, Type openRequestHandlerImplementation)
    {
        var closingTypes = concreteGenericTRequest.GetGenericArguments();

        var concreteInterfaces = concreteGenericTRequest.GetInterfaces();
        var concreteTResponse = (concreteInterfaces
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommand<>))
            ?? concreteInterfaces.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IQuery<>)))
            ?.GetGenericArguments()
            .FirstOrDefault();

        var typeDefinition = openRequestHandlerInterface.GetGenericTypeDefinition();

        var serviceType = concreteTResponse != null ?
            typeDefinition.MakeGenericType(concreteGenericTRequest, concreteTResponse) :
            typeDefinition.MakeGenericType(concreteGenericTRequest);

        return (serviceType, openRequestHandlerImplementation.MakeGenericType(closingTypes));
    }

    /// <summary>
    /// 依据泛型约束推断可闭合的请求类型集合;返回 null 表示放弃推断
    /// </summary>
    private static List<Type>? GetConcreteRequestTypes(Type openRequestHandlerInterface, Type openRequestHandlerImplementation, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        // 请求泛型类型的约束
        var constraintsForEachParameter = openRequestHandlerImplementation
            .GetGenericArguments()
            .Select(x => x.GetGenericParameterConstraints())
            .ToList();

        var typesThatCanCloseForEachParameter = constraintsForEachParameter
            .Select(constraints => assembliesToScan
                .SelectMany(assembly => assembly.GetLoadableDefinedTypes())
                .Where(type => type.IsClass && !type.IsAbstract && constraints.All(constraint => constraint.IsAssignableFrom(type))).ToList()
            ).ToList();

        var requestType = openRequestHandlerInterface.GenericTypeArguments.First();

        if (requestType.IsGenericParameter)
            return null;

        var requestGenericTypeDefinition = requestType.GetGenericTypeDefinition();
              
        var combinations = GenerateCombinations(requestType, typesThatCanCloseForEachParameter, 0, cancellationToken);

        return combinations.Select(types => requestGenericTypeDefinition.MakeGenericType(types.ToArray())).ToList();
    }

    // 递归生成类型组合的方法
    /// <summary>
    /// 递归生成各约束类型的全部组合
    /// </summary>
    public static List<List<Type>> GenerateCombinations(Type requestType, List<List<Type>> lists, int depth = 0, CancellationToken cancellationToken = default)
    {
        if (depth == 0)
        {
            // 初始检查
            if (MaxGenericTypeParameters > 0 && lists.Count > MaxGenericTypeParameters)
                throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The number of generic type parameters exceeds the maximum allowed ({MaxGenericTypeParameters}).");

            foreach (var list in lists)
            {
                if (MaxTypesClosing > 0 && list.Count > MaxTypesClosing)
                    throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. One of the generic type parameter's count of types that can close exceeds the maximum length allowed ({MaxTypesClosing}).");
            }

            // 计算组合总数
            long totalCombinations = 1;
            foreach (var list in lists)
            {
                totalCombinations *= list.Count;
                if (MaxGenericTypeRegistrations > 0 && totalCombinations > MaxGenericTypeRegistrations)
                    throw new ArgumentException($"Error registering the generic type: {requestType.FullName}. The total number of generic type registrations exceeds the maximum allowed ({MaxGenericTypeRegistrations}).");
            }
        }

        if (depth >= lists.Count)
            return new List<List<Type>> { new List<Type>() };
       
        cancellationToken.ThrowIfCancellationRequested();

        var currentList = lists[depth];
        var childCombinations = GenerateCombinations(requestType, lists, depth + 1, cancellationToken);
        var combinations = new List<List<Type>>();

        foreach (var item in currentList)
        {
            foreach (var childCombination in childCombinations)
            {
                var currentCombination = new List<Type> { item };
                currentCombination.AddRange(childCombination);
                combinations.Add(currentCombination);
            }
        }

        return combinations;
    }

    /// <summary>
    /// 把所有能闭合到指定接口的具体类型(含扫描到的请求类型)登记进容器
    /// </summary>
    private static void AddAllConcretionsThatClose(Type openRequestInterface, List<Type> concretions, IServiceCollection services, IEnumerable<Assembly> assembliesToScan, CancellationToken cancellationToken)
    {
        foreach (var concretion in concretions)
        {   
            var concreteRequests = GetConcreteRequestTypes(openRequestInterface, concretion, assembliesToScan, cancellationToken);

            if (concreteRequests is null)
                continue;

            var registrationTypes = concreteRequests
                .Select(concreteRequest => GetConcreteRegistrationTypes(openRequestInterface, concreteRequest, concretion));

            foreach (var (Service, Implementation) in registrationTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                services.AddTransient(Service, Implementation);
            }
        }
    }

    /// <summary>
    /// 判断开放实现能否闭合到给定接口
    /// </summary>
    internal static bool CouldCloseTo(this Type openConcretion, Type closedInterface)
    {
        var openInterface = closedInterface.GetGenericTypeDefinition();
        var arguments = closedInterface.GenericTypeArguments;

        var concreteArguments = openConcretion.GenericTypeArguments;
        return arguments.Length == concreteArguments.Length && openConcretion.CanBeCastTo(openInterface);
    }

    /// <summary>
    /// 判断类型能否赋值给指定的插件类型(含泛型定义匹配)
    /// </summary>
    private static bool CanBeCastTo(this Type pluggedType, Type pluginType)
    {
        if (pluggedType == null) return false;

        if (pluggedType == pluginType) return true;

        return pluginType.IsAssignableFrom(pluggedType);
    }

    /// <summary>
    /// 判断类型是否为开放泛型
    /// </summary>
    private static bool IsOpenGeneric(this Type type)
    {
        return type.IsGenericTypeDefinition || type.ContainsGenericParameters;
    }

    /// <summary>
    /// 查找该类型实现的、可闭合到指定模板的接口
    /// </summary>
    internal static IEnumerable<Type> FindInterfacesThatClose(this Type pluggedType, Type templateType)
    {
        return FindInterfacesThatClosesCore(pluggedType, templateType).Distinct();
    }

    /// <summary>
    /// 查找可闭合到指定模板的接口的具体实现逻辑
    /// </summary>
    private static IEnumerable<Type> FindInterfacesThatClosesCore(Type pluggedType, Type templateType)
    {
        if (pluggedType == null) yield break;

        if (!pluggedType.IsConcrete()) yield break;

        if (templateType.IsInterface)
        {
            foreach (
                var interfaceType in
                pluggedType.GetInterfaces()
                    .Where(type => type.IsGenericType && (type.GetGenericTypeDefinition() == templateType)))
            {
                yield return interfaceType;
            }
        }
        else if (pluggedType.BaseType!.IsGenericType &&
                 (pluggedType.BaseType!.GetGenericTypeDefinition() == templateType))
        {
            yield return pluggedType.BaseType!;
        }

        if (pluggedType.BaseType == typeof(object)) yield break;

        foreach (var interfaceType in FindInterfacesThatClosesCore(pluggedType.BaseType!, templateType))
        {
            yield return interfaceType;
        }
    }

    /// <summary>
    /// 判断类型是否为可实例化的具体类
    /// </summary>
    private static bool IsConcrete(this Type type)
    {
        return !type.IsAbstract && !type.IsInterface;
    }

    /// <summary>
    /// 把集合填充为指定值
    /// </summary>
    private static void Fill<T>(this IList<T> list, T value)
    {
        if (list.Contains(value)) return;
        list.Add(value);
    }

    /// <summary>
    /// 获取程序集中可加载的类型(忽略加载失败的类型)
    /// </summary>
    private static IEnumerable<Type> GetLoadableDefinedTypes(this Assembly assembly)
    {
        try
        {
            return assembly.DefinedTypes;
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

}
