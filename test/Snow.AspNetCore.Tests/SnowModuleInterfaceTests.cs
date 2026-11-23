using Microsoft.Extensions.DependencyInjection;
using Snow.Modularity;
using Xunit;

namespace Snow.AspNetCore.Tests;

/// <summary>
/// 模块接口: 成员与 ABP 的 IAbpModule 逐个对齐(只有服务注册中段两个方法),
/// 且「是不是模块」只看接口 —— 不继承 SnowModule 也算模块
/// </summary>
public class SnowModuleInterfaceTests
{
    /// <summary>
    /// 接口只有两个成员, 名字与签名与 ABP 的 IAbpModule 一致
    /// </summary>
    [Fact]
    public void Interface_HasOnlyTheTwoConfigureServicesMembers()
    {
        Type interfaceType = typeof(ISnowModule);

        Assert.True(interfaceType.IsInterface);
        Assert.Empty(interfaceType.GetProperties());
        Assert.Empty(interfaceType.GetEvents());

        System.Reflection.MethodInfo[] methods = interfaceType.GetMethods();
        Assert.Equal(2, methods.Length);

        System.Reflection.MethodInfo async = Assert.Single(methods, method => method.Name == nameof(ISnowModule.ConfigureServicesAsync));
        Assert.Equal(typeof(Task), async.ReturnType);
        Assert.Equal([typeof(ServiceConfigurationContext)], async.GetParameters().Select(parameter => parameter.ParameterType));

        System.Reflection.MethodInfo sync = Assert.Single(methods, method => method.Name == nameof(ISnowModule.ConfigureServices));
        Assert.Equal(typeof(void), sync.ReturnType);
        Assert.Equal([typeof(ServiceConfigurationContext)], sync.GetParameters().Select(parameter => parameter.ParameterType));
    }

    /// <summary>
    /// SnowModule 实现接口的两个方法: 异步版默认转发同步版, 重写任意一个都生效
    /// </summary>
    [Fact]
    public async Task SnowModule_ImplementsTheTwoMembers()
    {
        ISnowModule module = new SyncOverrideModule();

        await module.ConfigureServicesAsync(null!);

        Assert.Equal(1, SyncOverrideModule.SyncCalls);
    }

    /// <summary>
    /// 只实现接口、不继承 SnowModule 的类型也能当启动模块: 装配成功且中段被调用一次
    /// </summary>
    [Fact]
    public async Task AddApplicationAsync_AcceptsModuleImplementingOnlyTheInterface()
    {
        ServiceCollection services = new();

        SnowApplication application = await services.AddApplicationAsync<InterfaceOnlyModule>();

        Assert.Single(application.Modules);
        Assert.Equal(typeof(InterfaceOnlyModule), application.Modules[0].Type);
        Assert.Equal(1, InterfaceOnlyModule.SyncCalls);
    }

    /// <summary>
    /// 描述信息按接口暴露实例(与 ABP 的 AbpModuleDescriptor.Instance 同类型), 拿到具体类型要自己判
    /// </summary>
    [Fact]
    public void Descriptor_ExposesInstanceAsInterface()
    {
        SnowModuleDescriptor descriptor = new(typeof(SyncOverrideModule), new SyncOverrideModule());

        Assert.IsAssignableFrom<ISnowModule>(descriptor.Instance);
        Assert.IsType<SyncOverrideModule>(descriptor.Instance);
    }

    /// <summary>
    /// 校验只认接口: 没实现的类型被拒, 报错文案说「未实现」
    /// </summary>
    [Fact]
    public void CheckModuleType_RejectsTypeWithoutTheInterface()
    {
        ModuleLoadException exception = Assert.Throws<ModuleLoadException>(() => SnowModuleHelper.CheckModuleType(typeof(string)));

        Assert.Contains("未实现", exception.Message);
        Assert.Contains(nameof(ISnowModule), exception.Message);
    }

    private sealed class SyncOverrideModule : SnowModule
    {
        public static int SyncCalls { get; private set; }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            SyncCalls++;
        }
    }

    private sealed class InterfaceOnlyModule : ISnowModule
    {
        public static int SyncCalls { get; private set; }

        public Task ConfigureServicesAsync(ServiceConfigurationContext context)
        {
            ConfigureServices(context);
            return Task.CompletedTask;
        }

        public void ConfigureServices(ServiceConfigurationContext context)
        {
            SyncCalls++;
        }
    }
}
