using Microsoft.Extensions.Configuration;
using Snow.DependencyInjection;

namespace Snow.Features;

/// <summary>
/// 配置提供器: 从配置节 <c>Features:{功能名}</c> 读值
/// </summary>
[SingletonService]
public sealed class ConfigurationFeatureValueProvider : IFeatureValueProvider
{
    /// <summary>
    /// 配置节的公共前缀
    /// </summary>
    public const string ConfigurationPrefix = "Features";

    private readonly IConfiguration _configuration;

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="configuration">配置根</param>
    public ConfigurationFeatureValueProvider(IConfiguration configuration) => _configuration = configuration;

    /// <inheritdoc />
    public string Name => "C";

    /// <inheritdoc />
    public Task<string?> GetOrNullAsync(FeatureDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        string? value = _configuration[ConfigurationPrefix + ":" + definition.Name];
        return Task.FromResult(string.IsNullOrEmpty(value) ? null : value);
    }
}
