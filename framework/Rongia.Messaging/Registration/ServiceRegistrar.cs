using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Rongia.Messaging.Registration;

/// <summary>
/// 扫描程序集,把命令/查询处理器、通知处理器、管道行为、前置/后置处理器按约定注册到 DI 容器
/// </summary>
public static class ServiceRegistrar
{
    private static int MaxGenericTypeParameters;
    private static int MaxTypesClosing;
    private static int MaxGenericTypeRegistrations;
    private static int RegistrationTimeout; 

    /// <summary>
    /// 把配置中的泛型注册上限与超时写入静态字段
    /// </summary>
    /// <param name="configuration">中介器注册配置</param>
    public static void SetGenericRequestHandlerRegistrationLimitations(MessagingServiceConfiguration configuration)
    {
        MaxGenericTypeParameters = configuration.MaxGenericTypeParameters;
        MaxTypesClosing = configuration.MaxTypesClosing;
        MaxGenericTypeRegistrations = configuration.MaxGenericTypeRegistrations;
        RegistrationTimeout = configuration.RegistrationTimeout;
    }

    /// <summary>
    /// 带超时地执行全部注册
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">中介器注册配置</param>
    public static void AddMessagingClassesWithTimeout(IServiceCollection services, MessagingServiceConfiguration configuration)
    {
        // 超时设为 0 或负数表示禁用超时;否则 CancellationTokenSource 传入 0 会立刻取消
        using(var cts = new CancellationTokenSource(RegistrationTimeout > 0 ? RegistrationTimeout : Timeout.Infinite))
        {
            try
            {
                AddMessagingClasses(services, configuration, cts.Token);
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
    /// <param name="configuration">中介器注册配置</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static void AddMessagingClasses(IServiceCollection services, MessagingServiceConfiguration configuration, CancellationToken cancellationToken = default)
    {   

        var assembliesToScan = configuration.AssembliesToRegister.Distinct().ToArray();

        ConnectImplementationsToTypesClosing(typeof(ICommandHandler<,>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(ICommandHandler<>), services, assembliesToScan, false, configuration, cancellationToken);
        ConnectImplementationsToTypesClosing(typeof(IQueryHandler<,>), services, assembliesToScan, false, configuration, cancellationToken);
    }

    /// <summary>
    /// 把实现了指定开放接口的类型登记到容器
    /// </summary>
    private static void ConnectImplementationsToTypesClosing(Type openRequestInterface,
        IServiceCollection services,
        IEnumerable<Assembly> assembliesToScan,
        bool addIfAlreadyExists,
        MessagingServiceConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var concretions = new List<Type>();
        var interfaces = new List<Type>();
        var genericConcretions = new List<Type>();
        var genericInterfaces = new List<Type>();

        var types = assembliesToScan
            .SelectMany(a => a.GetLoadableDefinedTypes())
            .Where(t => !t.ContainsGenericParameters || configuration.RegisterGenericHandlers)
            .Where(t => t.IsConcrete() && t.FindInterfacesThatClose(openRequestInterface).Any())
            .Where(configuration.TypeEvaluator)
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

    // 同一个实现可能多次闭合到同一个服务接口,
    // 例如 DualBehavior<TResponse> : IPipelineBehavior<FooRequest, TResponse>, IPipelineBehavior<BarRequest, TResponse>
    /// <summary>
    /// 取开放实现类型针对指定开放服务实现的全部服务接口
    /// </summary>
    private static Type[] GetOpenServiceInterfaces(Type openImplementationType, Type openServiceType) =>
        openImplementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openServiceType)
            .ToArray();

    // DI 容器按位置闭合开放式泛型,只有当实现的类型参数按顺序一一对应服务接口时才可用(如 IPipelineBehavior<TRequest, TResponse>)。
    // 其它情况(嵌套泛型如 List<T>、多余的类型参数、顺序颠倒,或多次实现同一个服务接口)都必须显式闭合。
    /// <summary>
    /// 判断该实现是否必须显式闭合(容器按位置闭合不可用的情形)
    /// </summary>
    private static bool RequiresExplicitClosing(Type openImplementationType, Type openServiceType)
    {
        var interfaces = GetOpenServiceInterfaces(openImplementationType, openServiceType);
        if (interfaces.Length == 0) return false;
        return interfaces.Length > 1
            || !interfaces[0].GetGenericArguments().SequenceEqual(openImplementationType.GetGenericArguments());
    }

    // 按服务类型匹配来闭合开放式泛型的容器,只有在实现的每个类型参数都出现在服务接口实参中时才能闭合;
    // 仅出现在约束里的类型参数只能通过扫描请求类型来推断。
    /// <summary>
    /// 判断实现能否直接由服务类型闭合
    /// </summary>
    private static bool CanBeClosedFromServiceType(Type openImplementationType, Type openServiceType)
    {
        var interfaces = GetOpenServiceInterfaces(openImplementationType, openServiceType);
        if (interfaces.Length != 1) return false;
        var covered = new HashSet<Type>(interfaces[0].GetGenericArguments().SelectMany(GetGenericParametersIn));
        return openImplementationType.GetGenericArguments().All(covered.Contains);
    }

    // 把服务类型实参中的泛型参数与具体类型做结构化匹配来完成绑定;
    // 除参数以外的部分必须完全一致。
    /// <summary>
    /// 结构化匹配服务类型实参,收集泛型参数与具体类型的绑定
    /// </summary>
    private static bool TryMatchType(Type pattern, Type concrete, Dictionary<Type, Type> bindings)
    {
        if (pattern.IsGenericParameter)
        {
            if (bindings.TryGetValue(pattern, out var existing)) return existing == concrete;
            bindings[pattern] = concrete;
            return true;
        }
        if (pattern.IsArray)
        {
            return IsSameArrayShape(pattern, concrete)
                && TryMatchType(pattern.GetElementType()!, concrete.GetElementType()!, bindings);
        }
        if (pattern.IsGenericType && concrete.IsGenericType)
        {
            if (pattern.GetGenericTypeDefinition() != concrete.GetGenericTypeDefinition()) return false;
            var pArgs = pattern.GetGenericArguments();
            var cArgs = concrete.GetGenericArguments();
            if (pArgs.Length != cArgs.Length) return false;
            return pArgs.Zip(cArgs, (p, c) => TryMatchType(p, c, bindings)).All(x => x);
        }
        return pattern == concrete;
    }

    // 把约束与已绑定参数所实现的类型做结构化匹配,收集每个仍未知的参数可能对应的全部类型。
    // 已绑定的参数与固定实参直接接受,因为随后 MakeGenericType 会重新校验全部约束(含变体),
    // 例如当 IMarker 为协变时,实现 IMarker<AppDbContext> 的请求同样满足 where TRequest : IMarker<IDbContext>。
    // 在变体下同一个参数可能出现多次(IPair<TItem, TItem> 对 IPair<Derived, Base>),因此全部候选都会保留。
    // 嵌套实参也通过具体实参所实现的类型来匹配,因为协变允许 ICovariant<List<int>> 满足 ICovariant<IEnumerable<TItem>>。
    /// <summary>
    /// 结构化匹配约束,收集仍未知参数的全部候选类型
    /// </summary>
    private static bool TryCollectConstraintCandidates(
        Type pattern, Type concrete, IReadOnlyDictionary<Type, Type> known, Dictionary<Type, HashSet<Type>> candidates)
    {
        if (pattern.IsGenericParameter)
        {
            if (known.ContainsKey(pattern)) return true;
            if (!candidates.TryGetValue(pattern, out var types)) candidates[pattern] = types = new HashSet<Type>();
            types.Add(concrete);
            return true;
        }
        if (!pattern.ContainsGenericParameters) return true;
        if (pattern.IsArray)
        {
            return IsSameArrayShape(pattern, concrete)
                && TryCollectConstraintCandidates(pattern.GetElementType()!, concrete.GetElementType()!, known, candidates);
        }
        if (pattern.IsGenericType)
        {
            var definition = pattern.GetGenericTypeDefinition();
            var matched = false;
            foreach (var compatible in GetSelfBaseTypesAndInterfaces(concrete)
                         .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == definition))
            {
                var found = new Dictionary<Type, HashSet<Type>>();
                if (!pattern.GetGenericArguments().Zip(compatible.GetGenericArguments(),
                        (p, c) => TryCollectConstraintCandidates(p, c, known, found)).All(x => x)) continue;

                matched = true;
                MergeCandidates(candidates, found);
            }
            return matched;
        }
        return false;
    }

    /// <summary>
    /// 把 from 中的候选合并进 into
    /// </summary>
    private static void MergeCandidates(Dictionary<Type, HashSet<Type>> into, Dictionary<Type, HashSet<Type>> from)
    {
        foreach (var pair in from)
        {
            if (!into.TryGetValue(pair.Key, out var types)) into[pair.Key] = types = new HashSet<Type>();
            types.UnionWith(pair.Value);
        }
    }

    /// <summary>
    /// 判断两个数组类型的形状是否一致
    /// </summary>
    private static bool IsSameArrayShape(Type pattern, Type concrete) =>
        concrete.IsArray
        && pattern.GetArrayRank() == concrete.GetArrayRank()
        && IsSzArray(pattern) == IsSzArray(concrete);

    // T[] 与 T[*] 的秩都是 1,只有一维零基数组能通过 MakeArrayType() 往返还原
    /// <summary>
    /// 判断数组是否为一维零基(SZ)数组
    /// </summary>
    private static bool IsSzArray(Type arrayType) => arrayType == arrayType.GetElementType()!.MakeArrayType();

    /// <summary>
    /// 取类型中出现的全部泛型参数
    /// </summary>
    private static IEnumerable<Type> GetGenericParametersIn(Type type)
    {
        if (type.IsGenericParameter) return new[] { type };
        if (type.IsArray) return GetGenericParametersIn(type.GetElementType()!);
        if (!type.IsGenericType) return Enumerable.Empty<Type>();
        return type.GetGenericArguments().SelectMany(GetGenericParametersIn);
    }

    /// <summary>
    /// 取类型自身、基类以及实现的全部接口
    /// </summary>
    private static IEnumerable<Type> GetSelfBaseTypesAndInterfaces(Type type)
    {
        for (var current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            yield return current;
        }
        foreach (var @interface in type.GetInterfaces())
        {
            yield return @interface;
        }
    }

    /// <summary>
    /// 从约束链推导"既不在请求类型、也不在响应类型中"的行为类型参数候选值
    /// </summary>
    /// <remarks>
    /// 当约束把这些参数与已知参数绑在一起时,它们仍可被推断,例如
    /// <c>TransactionBehavior&lt;TRequest, TResponse, TDbContext&gt; where TRequest : ITransactionalCommand&lt;TDbContext&gt;</c>。
    /// 每一轮为每条链延长一节,轮数上限为类型参数个数,因此不会陷入约束环。
    /// </remarks>
    private static IEnumerable<Dictionary<Type, Type>> ExpandBindingsFromConstraints(
        Type[] behaviorParams, Dictionary<Type, Type> bindings)
    {
        var candidates = new Dictionary<Type, HashSet<Type>>();
        var pending = bindings.ToDictionary(pair => pair.Key, pair => new HashSet<Type> { pair.Value });

        for (var pass = 0; pass < behaviorParams.Length && pending.Count > 0; pass++)
        {
            var found = new Dictionary<Type, HashSet<Type>>();
            foreach (var pair in pending)
            {
                foreach (var constraint in pair.Key.GetGenericParameterConstraints()
                             .Where(c => GetGenericParametersIn(c).Any(p => !bindings.ContainsKey(p))))
                {
                    foreach (var candidate in pair.Value.SelectMany(GetSelfBaseTypesAndInterfaces))
                    {
                        var collected = new Dictionary<Type, HashSet<Type>>();
                        if (TryCollectConstraintCandidates(constraint, candidate, bindings, collected))
                        {
                            MergeCandidates(found, collected);
                        }
                    }
                }
            }

            pending = new Dictionary<Type, HashSet<Type>>();
            foreach (var pair in found)
            {
                if (!candidates.TryGetValue(pair.Key, out var types)) candidates[pair.Key] = types = new HashSet<Type>();
                var added = pair.Value.Where(types.Add).ToList();
                if (added.Count > 0) pending[pair.Key] = new HashSet<Type>(added);
            }
        }

        if (!behaviorParams.All(p => bindings.ContainsKey(p) || candidates.ContainsKey(p))) return Enumerable.Empty<Dictionary<Type, Type>>();

        return CombineCandidates(behaviorParams, bindings, candidates);
    }

    /// <summary>
    /// 为每个待推断参数各取一个候选类型,组合出全部可行绑定
    /// </summary>
    /// <remarks>
    /// 候选按参数汇总后已无法区分来源链,因此某个参数一旦可校验就立刻校验,不满足的组合直接丢弃。
    /// </remarks>
    private static IEnumerable<Dictionary<Type, Type>> CombineCandidates(
        Type[] behaviorParams, Dictionary<Type, Type> bindings, Dictionary<Type, HashSet<Type>> candidates)
    {
        var order = candidates.Keys.ToList();
        var notYetCheckable = behaviorParams.Where(p => !IsCheckable(p, bindings)).ToList();
        return Assign(0, bindings, notYetCheckable);

        IEnumerable<Dictionary<Type, Type>> Assign(int index, Dictionary<Type, Type> partial, List<Type> uncheckedParams)
        {
            if (index == order.Count)
            {
                yield return partial;
                yield break;
            }

            foreach (var type in candidates[order[index]])
            {
                var extended = new Dictionary<Type, Type>(partial) { [order[index]] = type };
                var nowCheckable = uncheckedParams.Where(p => IsCheckable(p, extended)).ToList();
                if (!nowCheckable.All(p => SatisfiesTypeConstraints(p, extended))) continue;

                foreach (var result in Assign(index + 1, extended, uncheckedParams.Except(nowCheckable).ToList()))
                {
                    yield return result;
                }
            }
        }
    }

    /// <summary>
    /// 该参数及其约束引用的参数是否都已绑定
    /// </summary>
    private static bool IsCheckable(Type param, IReadOnlyDictionary<Type, Type> bindings) =>
        bindings.ContainsKey(param)
        && param.GetGenericParameterConstraints().SelectMany(GetGenericParametersIn).All(bindings.ContainsKey);

    /// <summary>
    /// 只拒绝 MakeGenericType 也会拒绝的组合,特殊约束(class/struct/new)交给 MakeGenericType
    /// </summary>
    private static bool SatisfiesTypeConstraints(Type param, IReadOnlyDictionary<Type, Type> bindings)
    {
        foreach (var constraint in param.GetGenericParameterConstraints())
        {
            Type closedConstraint;
            try
            {
                closedConstraint = Substitute(constraint, bindings);
            }
            catch (ArgumentException)
            {
                continue;
            }
            if (!closedConstraint.IsAssignableFrom(bindings[param])) return false;
        }
        return true;
    }

    /// <summary>
    /// 按绑定表替换类型中的泛型参数
    /// </summary>
    private static Type Substitute(Type type, IReadOnlyDictionary<Type, Type> bindings)
    {
        if (type.IsGenericParameter) return bindings[type];
        if (!type.ContainsGenericParameters) return type;
        if (type.IsArray)
        {
            var element = Substitute(type.GetElementType()!, bindings);
            return IsSzArray(type) ? element.MakeArrayType() : element.MakeArrayType(type.GetArrayRank());
        }
        return type.GetGenericTypeDefinition().MakeGenericType(type.GetGenericArguments().Select(a => Substitute(a, bindings)).ToArray());
    }

    /// <summary>
    /// 校验开放式实现的每个类型参数都可由服务接口参数或约束链推断出来
    /// </summary>
    /// <exception cref="InvalidOperationException">存在无法推断的类型参数</exception>
    private static void ValidateTypeParametersCanBeInferred(Type openImplementationType, Type iface)
    {
        var reachable = new HashSet<Type>(iface.GetGenericArguments().SelectMany(GetGenericParametersIn));
        var added = true;
        while (added)
        {
            added = false;
            foreach (var tp in reachable.ToList())
            {
                foreach (var p in tp.GetGenericParameterConstraints().SelectMany(GetGenericParametersIn))
                {
                    added |= reachable.Add(p);
                }
            }
        }

        var unreachable = openImplementationType.GetGenericArguments().Where(tp => !reachable.Contains(tp)).ToList();
        if (unreachable.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot register {openImplementationType.Name}: type parameter(s) {string.Join(", ", unreachable.Select(t => t.Name))} " +
                $"do not appear in the {iface.Name} arguments or in a constraint linked to them, so they cannot be inferred.");
        }
    }

    /// <summary>
    /// 扫描已注册程序集,找出所有命令与查询的(请求类型, 响应类型)对
    /// </summary>
    private static IEnumerable<(Type RequestType, Type ResponseType)> FindRequestTypes(IEnumerable<Assembly> assemblies) =>
        assemblies
            .SelectMany(a => a.GetLoadableDefinedTypes())
            .Where(t => t.IsConcrete() && !t.ContainsGenericParameters)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i == typeof(ICommand) || (i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICommand<>) || i.GetGenericTypeDefinition() == typeof(IQuery<>))))
                .Select(i => (RequestType: t, ResponseType: i == typeof(ICommand) ? typeof(Unit) : i.GetGenericArguments()[0])))
            .ToList();

    /// <summary>
    /// 为每个匹配到的请求逐个闭合开放式实现并注册
    /// </summary>
    private static void RegisterClosedImplementations(
        Type openImplementationType, Type iface, IServiceCollection services,
        IEnumerable<(Type RequestType, Type ResponseType)> requests, ServiceLifetime lifetime)
    {
        // 单参数服务(如泛型单类型参数接口)只按请求类型匹配
        var patterns   = iface.GetGenericArguments();
        var implParams = openImplementationType.GetGenericArguments();
        var openServiceType = iface.GetGenericTypeDefinition();

        foreach (var (requestType, responseType) in requests)
        {
            var serviceArgs = patterns.Length == 1 ? new[] { requestType } : new[] { requestType, responseType };
            var bindings = new Dictionary<Type, Type>();
            if (!patterns.Zip(serviceArgs, (p, c) => TryMatchType(p, c, bindings)).All(x => x)) continue;

            var closedService = openServiceType.MakeGenericType(serviceArgs);
            var closedImplementations = new HashSet<Type>();
            foreach (var complete in ExpandBindingsFromConstraints(implParams, bindings))
            {
                try
                {
                    var closedImplementation = openImplementationType.MakeGenericType(implParams.Select(tp => complete[tp]).ToArray());
                    if (closedImplementations.Add(closedImplementation))
                    {
                        services.TryAddEnumerable(new ServiceDescriptor(closedService, closedImplementation, lifetime));
                    }
                }
                catch (ArgumentException) { /* 该请求不满足泛型约束,跳过 */ }
            }
        }
    }

    /// <summary>
    /// 判断一条注册是否需要按扫描到的请求显式闭合
    /// </summary>
    private static bool RequiresExplicitClosing(ServiceDescriptor serviceDescriptor, Type openServiceType, OpenBehaviorClosingStrategy closingStrategy) =>
        // 键控注册属于调用方自己,原样保留
        !serviceDescriptor.IsKeyedService
        && serviceDescriptor.ImplementationType != null
        && serviceDescriptor.ServiceType == openServiceType
        && serviceDescriptor.ImplementationType.IsOpenGeneric()
        && RequiresExplicitClosing(serviceDescriptor.ImplementationType, openServiceType)
        && !(closingStrategy == OpenBehaviorClosingStrategy.CloseInContainer
             && CanBeClosedFromServiceType(serviceDescriptor.ImplementationType, openServiceType));

    /// <summary>
    /// 注册开放式泛型服务:容器无法按位置闭合的按扫描请求逐个闭合,其余交给容器
    /// </summary>
    private static void RegisterOpenGenericServices(
        IServiceCollection services, IEnumerable<ServiceDescriptor> serviceDescriptors, Type openServiceType,
        Lazy<IEnumerable<(Type RequestType, Type ResponseType)>> requests, OpenBehaviorClosingStrategy closingStrategy)
    {
        foreach (var serviceDescriptor in serviceDescriptors)
        {
            if (RequiresExplicitClosing(serviceDescriptor, openServiceType, closingStrategy))
            {
                foreach (var iface in GetOpenServiceInterfaces(serviceDescriptor.ImplementationType!, openServiceType))
                {
                    ValidateTypeParametersCanBeInferred(serviceDescriptor.ImplementationType!, iface);
                    RegisterClosedImplementations(serviceDescriptor.ImplementationType!, iface, services, requests.Value, serviceDescriptor.Lifetime);
                }
                continue;
            }

            services.TryAddEnumerable(serviceDescriptor);
        }
    }

    /// <summary>
    /// 注册中介者自身所需的服务
    /// </summary>
    /// <remarks>
    /// 全部使用 TryAdd,调用方已注册的总线实现不会被覆盖。
    /// </remarks>
    public static void AddRequiredServices(IServiceCollection services, MessagingServiceConfiguration serviceConfiguration)
    {
        services.TryAddSingleton(serviceConfiguration);

        var requests = new Lazy<IEnumerable<(Type RequestType, Type ResponseType)>>(() => FindRequestTypes(serviceConfiguration.AssembliesToRegister));

        RegisterOpenGenericServices(services, serviceConfiguration.BehaviorsToRegister, typeof(IPipelineBehavior<,>), requests, serviceConfiguration.OpenBehaviorClosingStrategy);
    }
}
