using Mapster;
using Snow.DependencyInjection;

namespace Snow.ObjectMapping.Mapster;

/// <summary>
/// 基于 Mapster 的对象映射器: 一切映射都走模块里建好的那份配置
/// </summary>
[TransientService(ReplaceServices = true)]
public sealed class MapsterObjectMapper : IObjectMapper
{
    private readonly TypeAdapterConfig _config;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="config">映射配置</param>
    public MapsterObjectMapper(TypeAdapterConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <inheritdoc />
    public TDestination? Map<TDestination>(object? source)
    {
        if (source is null)
        {
            return default;
        }

        return source.Adapt<TDestination>(_config);
    }

    /// <inheritdoc />
    public TDestination? Map<TSource, TDestination>(TSource? source)
    {
        if (source is null)
        {
            return default;
        }

        return source.Adapt<TSource, TDestination>(_config);
    }

    /// <inheritdoc />
    public TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination)
    {
        if (source is null)
        {
            return destination;
        }

        return destination is null
            ? source.Adapt<TSource, TDestination>(_config)
            : source.Adapt(destination, _config);
    }

    /// <inheritdoc />
    public object? Map(object? source, Type sourceType, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(destinationType);

        if (source is null)
        {
            return null;
        }

        return source.Adapt(sourceType, destinationType, _config);
    }

    /// <inheritdoc />
    public object? Map(object? source, object? destination, Type sourceType, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(destinationType);

        if (source is null)
        {
            return destination;
        }

        return destination is null
            ? source.Adapt(sourceType, destinationType, _config)
            : source.Adapt(destination, sourceType, destinationType, _config);
    }
}
