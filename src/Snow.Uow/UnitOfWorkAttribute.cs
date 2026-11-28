using System.Reflection;

namespace Snow.Uow;

/// <summary>
/// 声明式工作单元(对齐 ABP 的 <c>UnitOfWorkAttribute</c>): 标在类、接口或方法上, 被拦截的调用自动运行在一个工作单元里。
/// <para>方法正常返回即提交, 抛异常即回滚; 嵌套调用复用最外层的工作单元, 只有最外层提交。</para>
/// <para>本特性只是元数据, 真正干活的是 <see cref="UnitOfWorkInterceptor"/>, 由 <see cref="UnitOfWorkInterceptorRegistrar"/> 按它的存在挂上去。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = false)]
public sealed class UnitOfWorkAttribute : Attribute
{
    /// <summary>
    /// 是否开启数据库事务(默认开启); 设为 false 表示只借用工作单元做统一保存, 不包事务
    /// </summary>
    public bool IsTransactional { get; set; } = true;

    /// <summary>
    /// 是否忽略当前工作单元强制新开一个(新开的那层自己提交)
    /// </summary>
    public bool RequiresNew { get; set; }

    /// <summary>
    /// 显式关掉工作单元: 方法上标了它就不开
    /// <para>常用于类上统一标了 <c>[UnitOfWork]</c>、只有个别方法(如只读查询)不开的场合。</para>
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 取方法或它所在类型/接口上的 <see cref="UnitOfWorkAttribute"/>: 方法上的优先, 其次声明类型, 最后是同签名的接口方法
    /// </summary>
    /// <param name="method">被拦截的方法(通常是实现方法)</param>
    /// <returns>特性; 没有时返回 null</returns>
    public static UnitOfWorkAttribute? GetAttributeOrNull(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        return method.GetCustomAttribute<UnitOfWorkAttribute>(true)
            ?? method.DeclaringType?.GetCustomAttribute<UnitOfWorkAttribute>(true)
            ?? FindOnInterfaces(method);
    }

    /// <summary>
    /// 在实现类型实现的接口上找同名同参数的方法或接口本身的特性
    /// <para>拦截器拿到的是实现方法, 所以标在接口方法上的特性必须靠这一层兜底才能命中。</para>
    /// </summary>
    /// <param name="method">实现方法</param>
    /// <returns>特性; 没有时返回 null</returns>
    private static UnitOfWorkAttribute? FindOnInterfaces(MethodInfo method)
    {
        Type? declaringType = method.DeclaringType;
        if (declaringType is null)
        {
            return null;
        }

        int parameterCount = method.GetParameters().Length;

        foreach (Type contract in declaringType.GetInterfaces())
        {
            foreach (MethodInfo contractMethod in contract.GetMethods())
            {
                if (contractMethod.Name != method.Name || contractMethod.GetParameters().Length != parameterCount)
                {
                    continue;
                }

                return contractMethod.GetCustomAttribute<UnitOfWorkAttribute>(true)
                    ?? contract.GetCustomAttribute<UnitOfWorkAttribute>(true);
            }
        }

        return null;
    }
}
