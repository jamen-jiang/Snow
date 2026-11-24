namespace Microsoft.Extensions.Localization;

/// <summary>
/// <see cref="IStringLocalizerFactory"/> 扩展(对应 ABP 的 <c>AbpCoreStringLocalizerFactoryExtensions</c>)。
/// </summary>
public static class SnowCoreStringLocalizerFactoryExtensions
{
    /// <summary>
    /// 按资源类型创建本地化器。
    /// </summary>
    /// <param name="localizerFactory">本地化资源工厂</param>
    public static IStringLocalizer Create<TResource>(this IStringLocalizerFactory localizerFactory)
    {
        return localizerFactory.Create(typeof(TResource));
    }
}
