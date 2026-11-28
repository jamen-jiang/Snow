using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.Messaging.Registration;

/// <summary>
/// 命令/查询处理器的约定登记器(对应 ABP 的 <c>AbpRepositoryConventionalRegistrar</c> 那一类):
/// 只接管实现了处理器契约的具体类, 生命周期默认瞬态, 暴露类型取处理器接口。
/// <para>处理器因此不用写服务注解也能走进约定注册管道 —— 模块在 <c>OnRegistered</c> 挂的拦截器登记动作
/// (如工作单元)才问得到它, 从而像 ABP 的 <c>ApplicationService</c> 那样自动带事务。</para>
/// </summary>
public sealed class CommandHandlerConventionalRegistrar : ConventionalRegistrarBase
{
    /// <summary>
    /// 处理器契约的开放泛型定义
    /// </summary>
    private static readonly Type[] HandlerInterfaceDefinitions =
    {
        typeof(ICommandHandler<,>),
        typeof(ICommandHandler<>),
        typeof(IQueryHandler<,>),
    };

    /// <inheritdoc />
    protected override bool IsConventionalRegistrationDisabled(Type type)
    {
        return !IsHandler(type) || base.IsConventionalRegistrationDisabled(type);
    }

    /// <inheritdoc />
    protected override ServiceLifetime? GetLifeTimeOrNull(Type type)
    {
        return ServiceLifetime.Transient;
    }

    /// <inheritdoc />
    protected override Type[] GetExposedServiceTypes(Type type)
    {
        return type.GetInterfaces()
            .Where(interfaceType => interfaceType.IsGenericType
                && HandlerInterfaceDefinitions.Contains(interfaceType.GetGenericTypeDefinition()))
            .ToArray();
    }

    /// <summary>
    /// 是否实现了处理器契约
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>是处理器时返回 <see langword="true"/></returns>
    private static bool IsHandler(Type type)
    {
        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType && HandlerInterfaceDefinitions.Contains(interfaceType.GetGenericTypeDefinition()))
            {
                return true;
            }
        }

        return false;
    }
}
