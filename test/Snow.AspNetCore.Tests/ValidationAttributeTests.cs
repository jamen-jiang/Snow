using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snow.Castle;

using ServiceProvider = Snow.AspNetCore.Tests.TestServiceProvider;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Validation;
using ValidationAttribute = Snow.Validation.ValidationAttribute;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// [Validation] 注解三条触发路径: 类上贴整类校验、方法上贴单方法校验、没贴不校验。
/// </summary>
public class ValidationAttributeTests
{
    /// <summary>
    /// 类上贴 [Validation]: 所有公开方法都校验参数
    /// </summary>
    [Fact]
    public async Task ClassLevelValidationAttribute_ValidatesArguments()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        IClassValidatedService service = provider.GetRequiredService<IClassValidatedService>();

        Assert.Throws<SnowValidationException>(() => service.Run(new ProbeDto()));
    }

    /// <summary>
    /// 方法上贴 [Validation]: 只校验该方法
    /// </summary>
    [Fact]
    public async Task MethodLevelValidationAttribute_ValidatesArguments()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        IMethodValidatedService service = provider.GetRequiredService<IMethodValidatedService>();

        Assert.Throws<SnowValidationException>(() => service.Run(new ProbeDto()));
    }

    /// <summary>
    /// 没贴 [Validation]: 不校验
    /// </summary>
    [Fact]
    public async Task WithoutValidationAttribute_IsNotValidated()
    {
        using ServiceProvider provider = await BuildProviderAsync();

        provider.GetRequiredService<IPlainService>().Run(new ProbeDto());
    }

    private static async Task<ServiceProvider> BuildProviderAsync()
    {
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        await services.AddApplicationAsync<ValidationTestModule>();
        return new ServiceProvider(services.BuildServiceProviderFromFactory());
    }

    /// <summary>
    /// 测试模块
    /// </summary>
    [DependsOn(typeof(SnowValidationModule))]
    [DependsOn(typeof(SnowCastleCoreModule))]

    private sealed class ValidationTestModule : SnowModule
    {
    }

    /// <summary>
    /// 待校验对象: 必填项缺失, 校验必定报错
    /// </summary>
    public sealed class ProbeDto
    {
        /// <summary>必填名称</summary>
        [Required]
        public string? Name { get; set; }
    }

    /// <summary>类级特性契约</summary>
    public interface IClassValidatedService
    {
        /// <summary>执行</summary>
        void Run(ProbeDto argument);
    }

    /// <summary>类级特性实现</summary>
    [Validation]
    [TransientService]
    public class ClassValidatedService : IClassValidatedService
    {
        /// <inheritdoc />
        public virtual void Run(ProbeDto argument)
        {
        }
    }

    /// <summary>方法级特性契约</summary>
    public interface IMethodValidatedService
    {
        /// <summary>执行</summary>
        void Run(ProbeDto argument);
    }

    /// <summary>方法级特性实现</summary>
    [TransientService]
    public class MethodValidatedService : IMethodValidatedService
    {
        /// <inheritdoc />
        [Validation]
        public virtual void Run(ProbeDto argument)
        {
        }
    }

    /// <summary>无特性契约</summary>
    public interface IPlainService
    {
        /// <summary>执行</summary>
        void Run(ProbeDto argument);
    }

    /// <summary>无特性实现</summary>
    [TransientService]
    public class PlainService : IPlainService
    {
        /// <inheritdoc />
        public virtual void Run(ProbeDto argument)
        {
        }
    }
}
