using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.DependencyInjection;
using Snow.Domain;
using Snow.Domain.Services;
using Snow.Modularity;
using Snow.Timing;
using Xunit;
using ServiceProvider = Snow.AspNetCore.Tests.TestServiceProvider;

namespace Snow.AspNetCore.Tests;

public class DomainServiceTests
{
    [Fact]
    public async Task DomainService_GetsDependenciesFromConstructor()
    {
        using ServiceProvider provider = await BuildProviderAsync();
        using IServiceScope scope = provider.CreateScope();

        ProbeDomainService domainService = scope.ServiceProvider.GetRequiredService<ProbeDomainService>();

        Assert.NotNull(domainService.ReadClock());
        Assert.NotNull(domainService.ReadLogger());
    }

    [Fact]
    public async Task DomainService_IsRegisteredByConvention()
    {
        using ServiceProvider provider = await BuildProviderAsync();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsAssignableFrom<IDomainService>(scope.ServiceProvider.GetRequiredService<ProbeDomainService>());
    }

    private static async Task<ServiceProvider> BuildProviderAsync()
    {
        ServiceCollection services = new();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();

        await services.AddApplicationAsync<DomainTestModule>();

        return new ServiceProvider(services.BuildServiceProviderFromFactory());
    }

    [DependsOn(typeof(SnowDomainModule))]
    private sealed class DomainTestModule : SnowModule
    {
    }

    /// <summary>
    /// 探针领域服务: 依赖直接写在构造器上, 基类不提供任何属性
    /// </summary>
    [TransientService]
    public sealed class ProbeDomainService : DomainService
    {
        private readonly IClock _clock;
        private readonly ILoggerFactory _loggerFactory;

        public ProbeDomainService(IClock clock, ILoggerFactory loggerFactory)
        {
            _clock = clock;
            _loggerFactory = loggerFactory;
        }

        public IClock ReadClock() => _clock;

        public ILogger ReadLogger() => _loggerFactory.CreateLogger(GetType().FullName!);
    }
}
