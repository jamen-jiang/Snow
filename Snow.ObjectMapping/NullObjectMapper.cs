using Snow.DependencyInjection;

namespace Snow.ObjectMapping;

/// <summary>
/// 静默实现: 没有引入实现包时解析到它, 一调用就抛异常说明该装哪个包
/// <para>故意不返回空对象, 否则映射丢失会被当成正常结果悄悄放过。</para>
/// </summary>
[TransientService]
public sealed class NullObjectMapper : IObjectMapper
{
    private const string Message = "对象映射未执行: 当前是静默实现, 请引入 Snow.ObjectMapping.Mapster 包。";

    /// <inheritdoc />
    public TDestination? Map<TDestination>(object? source) => throw new SnowObjectMappingException(Message);

    /// <inheritdoc />
    public TDestination? Map<TSource, TDestination>(TSource? source) => throw new SnowObjectMappingException(Message);

    /// <inheritdoc />
    public TDestination? Map<TSource, TDestination>(TSource? source, TDestination? destination) => throw new SnowObjectMappingException(Message);

    /// <inheritdoc />
    public object? Map(object? source, Type sourceType, Type destinationType) => throw new SnowObjectMappingException(Message);

    /// <inheritdoc />
    public object? Map(object? source, object? destination, Type sourceType, Type destinationType) => throw new SnowObjectMappingException(Message);
}
