using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Snow.Json.SystemTextJson;
using Snow.Json.SystemTextJson.JsonConverters;
using Snow.Json.SystemTextJson.Modifiers;

namespace Snow.AspNetCore.Mvc.Json;

/// <summary>
/// MVC Core 的 JSON 配置扩展(对应 ABP 的 <c>MvcCoreBuilderExtensions</c>)。
/// </summary>
public static class MvcCoreBuilderExtensions
{
    /// <summary>
    /// 把 Snow 的 System.Text.Json 约定应用到 MVC Core。
    /// </summary>
    /// <param name="builder">MVC Core 构建器</param>
    /// <returns>构建器本身, 便于链式调用</returns>
    public static IMvcCoreBuilder AddSnowJson(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddOptions<JsonOptions>()
            .Configure<IServiceProvider>((options, rootServiceProvider) =>
            {
                options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                options.JsonSerializerOptions.AllowTrailingCommas = true;

                options.JsonSerializerOptions.Converters.Add(new SnowStringToEnumFactory());
                options.JsonSerializerOptions.Converters.Add(new SnowStringToBooleanConverter());
                options.JsonSerializerOptions.Converters.Add(new SnowStringToGuidConverter());
                options.JsonSerializerOptions.Converters.Add(new SnowNullableStringToGuidConverter());
                options.JsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());

                options.JsonSerializerOptions.TypeInfoResolver = new SnowDefaultJsonTypeInfoResolver(
                    rootServiceProvider.GetRequiredService<IOptions<SnowSystemTextJsonSerializerModifiersOptions>>());

                SnowDateTimeConverter dateTimeConverter = rootServiceProvider.GetRequiredService<SnowDateTimeConverter>();
                SnowNullableDateTimeConverter nullableDateTimeConverter = rootServiceProvider.GetRequiredService<SnowNullableDateTimeConverter>();

                options.JsonSerializerOptions.TypeInfoResolver
                    .As<SnowDefaultJsonTypeInfoResolver>()
                    .Modifiers.Add(new SnowDateTimeConverterModifier(dateTimeConverter, nullableDateTimeConverter).CreateModifyAction());
            });

        return builder;
    }
}
