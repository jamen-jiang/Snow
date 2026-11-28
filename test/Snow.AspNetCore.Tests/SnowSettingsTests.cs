using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;
using Snow.Security.Encryption;
using Snow.Settings;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;
using ServiceProvider = Snow.AspNetCore.Tests.TestServiceProvider;

namespace Snow.AspNetCore.Tests;

public class SnowSettingsTests
{
    private const string DefaultSetting = "Test.Default";
    private const string OverriddenSetting = "Test.Overridden";
    private const string SecretSetting = "Test.Secret";
    private const string DeletedSetting = "Test.Deleted";

    [Fact]
    public async Task GetOrNullAsync_UsesConfigurationBeforeDefault()
    {
        using ServiceProvider provider = await BuildProviderAsync(new Dictionary<string, string?>
        {
            ["Settings:" + OverriddenSetting] = "configured"
        });
        ISettingProvider settings = provider.GetRequiredService<ISettingProvider>();

        Assert.Equal("configured", await settings.GetOrNullAsync(OverriddenSetting));
        Assert.Equal("default-value", await settings.GetOrNullAsync(DefaultSetting));
    }

    [Fact]
    public async Task GetOrNullAsync_ReturnsNullForUnknownAndDeletedDefinitions()
    {
        using ServiceProvider provider = await BuildProviderAsync(configure: options =>
        {
            options.DeletedSettings.Add(DeletedSetting);
        });
        ISettingProvider settings = provider.GetRequiredService<ISettingProvider>();

        Assert.Null(await settings.GetOrNullAsync("Test.Unknown"));
        Assert.Null(await settings.GetOrNullAsync(DeletedSetting));
    }

    [Fact]
    public async Task GetOrNullAsync_DecryptsEncryptedValue()
    {
        StringEncryptionService encryption = new(MsOptions.Create(new SnowStringEncryptionOptions()));
        string cipherText = encryption.Encrypt("plain-secret")!;
        using ServiceProvider provider = await BuildProviderAsync(new Dictionary<string, string?>
        {
            ["Settings:" + SecretSetting] = cipherText
        });
        ISettingProvider settings = provider.GetRequiredService<ISettingProvider>();

        Assert.Equal("plain-secret", await settings.GetOrNullAsync(SecretSetting));
    }

    private static async Task<ServiceProvider> BuildProviderAsync(
        IReadOnlyDictionary<string, string?>? values = null,
        Action<SnowSettingOptions>? configure = null)
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build());
        services.AddLogging();

        await services.AddApplicationAsync<SettingsTestModule>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return new ServiceProvider(services.BuildServiceProviderFromFactory());
    }

    [DependsOn(typeof(SnowSettingsModule))]
    private sealed class SettingsTestModule : SnowModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<SnowSettingOptions>(options =>
            {
                options.DefinitionProviders.Add<TestSettingDefinitionProvider>();
            });
        }
    }

    private sealed class TestSettingDefinitionProvider : SettingDefinitionProvider
    {
        public override void Define(SettingDefinitionContext context)
        {
            context.Add(
                new SettingDefinition(DefaultSetting, "default-value"),
                new SettingDefinition(OverriddenSetting, "default-value"),
                new SettingDefinition(SecretSetting, isEncrypted: true),
                new SettingDefinition(DeletedSetting, "deleted-default"));
        }
    }
}
