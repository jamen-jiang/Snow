using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Snow.DependencyInjection;

/// <summary>
/// 约定登记器基类(对应 ABP 的 <c>ConventionalRegistrarBase</c>): 提供「遍历类型 → 过滤范围 → 定生命周期 → 取暴露服务类型」的默认流程。
/// <para>子类一般只覆写三个钩子: <see cref="IsConventionalRegistrationDisabled"/> 限定类型范围、
/// <see cref="GetLifeTimeOrNull"/> 给兜底生命周期、<see cref="GetExposedServiceTypes"/> 指定暴露的契约接口。</para>
/// </summary>
public abstract class ConventionalRegistrarBase : IConventionalRegistrar
{
    /// <inheritdoc />
    public virtual void AddAssembly(IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        List<Type> types = new();

        foreach (Type type in AssemblyTypeCache.GetTypes(assembly))
        {
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                continue;
            }

            types.Add(type);
        }

        AddTypes(services, types.ToArray());
    }

    /// <inheritdoc />
    public virtual void AddTypes(IServiceCollection services, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(types);

        foreach (Type type in types)
        {
            AddType(services, type);
        }
    }

    /// <inheritdoc />
    public virtual void AddType(IServiceCollection services, Type type)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
        {
            return;
        }

        if (IsConventionalRegistrationDisabled(type))
        {
            return;
        }

        ServiceLifetime? lifetime = GetLifeTimeOrNull(type);

        if (lifetime is null)
        {
            return;
        }

        Type[] serviceTypes = GetExposedServiceTypes(type);

        if (serviceTypes.Length == 0)
        {
            return;
        }

        services.AddExternalConventionalRegistration(
            new ExternalConventionalRegistration(type, lifetime.Value, serviceTypes, IsReplacing(type)));
    }

    /// <summary>
    /// 是否跳过该类型(默认: 标了 <see cref="DisableConventionalRegistrationAttribute"/> 的跳过)
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>跳过时返回 <see langword="true"/></returns>
    protected virtual bool IsConventionalRegistrationDisabled(Type type)
    {
        return type.IsDefined(typeof(DisableConventionalRegistrationAttribute), true);
    }

    /// <summary>
    /// 生命周期; 返回 null 表示本登记器不接管该类型
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>生命周期</returns>
    protected virtual ServiceLifetime? GetLifeTimeOrNull(Type type)
    {
        return null;
    }

    /// <summary>
    /// 暴露的服务类型(默认: 类型实现的全部接口; 通常应覆写成只取约定的契约接口)
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>服务类型数组</returns>
    protected virtual Type[] GetExposedServiceTypes(Type type)
    {
        return type.GetInterfaces();
    }

    /// <summary>
    /// 是否要求替换同服务类型的既有登记
    /// </summary>
    /// <param name="type">实现类型</param>
    /// <returns>需要替换时返回 <see langword="true"/></returns>
    protected virtual bool IsReplacing(Type type)
    {
        return false;
    }
}
