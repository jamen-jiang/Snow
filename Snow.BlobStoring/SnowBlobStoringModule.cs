using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;

namespace Snow.BlobStoring;

/// <summary>
/// 对象存储模块(对应 ABP 的 AbpBlobStoringModule)
/// <para>登记后端与开放泛型容器; 默认后端是内存实现, 需要持久化时用 AddSnowBlobStoring 换掉。</para>
/// </summary>
public sealed class SnowBlobStoringModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SnowBlobStoringOptions options = new();

        IConfigurationSection section = context.Configuration.GetSection("BlobStoring");
        if (section.Exists())
        {
            section.Bind(options);

            // ProviderType 是 Type, 配置绑定器不会替你转换字符串, 这里显式解析
            string? providerTypeName = section["ProviderType"];
            if (!string.IsNullOrWhiteSpace(providerTypeName))
            {
                options.ProviderType = ResolveProviderType(providerTypeName);
            }
        }

        context.Services.GetSingletonInstanceOrNull<SnowBlobStoringOptionsSetup>()?.Apply(options);

        context.Services.AddSingleton(options);
        context.Services.AddSingleton(typeof(IBlobProvider), options.ProviderType);
        context.Services.AddTransient(typeof(IBlobContainer<>), typeof(BlobContainer<>));
    }

    private static Type ResolveProviderType(string typeName)
    {
        Type? type = Type.GetType(typeName, throwOnError: false);

        if (type is null)
        {
            type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName, throwOnError: false))
                .FirstOrDefault(candidate => candidate is not null);
        }

        return type ?? throw new SnowInitializationException(
            $"BlobStoring:ProviderType 无法解析为类型: {typeName}。请写成 \"命名空间.类型, 程序集名\"。");
    }
}
