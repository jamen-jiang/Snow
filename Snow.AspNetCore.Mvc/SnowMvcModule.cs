using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;
using Snow.Modularity;
using Snow.Validation;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 与自动 API 模块: 应用服务按约定自动暴露成 Controller
/// <para>对应 ABP 的 <c>AbpAspNetCoreMvcModule</c>。三件事: 把应用服务登记成 Controller(特性提供器)、
/// 按约定算路由与动词(应用模型约定)、统一结果与异常响应(全局过滤器)。</para>
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: 自动 API 跑在它提供的 HTTP 上下文之上。</para>
/// <para>依赖 <see cref="SnowValidationModule"/>: 自动校验过滤器用 <see cref="ISnowObjectValidator"/> 跑 DataAnnotations 与 FluentValidation,
/// 与 ABP 把 <c>AbpValidationActionFilter</c> 放在 Volo.Abp.AspNetCore.Mvc 里同形 —— 校验只有一个包, 不需要 AspNetCore 后缀的校验包。</para>
/// </summary>
[DependsOn(typeof(SnowAspNetCoreModule), typeof(SnowValidationModule))]
public sealed class SnowMvcModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        SnowMvcOptions effective = BuildOptions(context.Services);
        context.Services.AddSingleton(effective);

        // 过滤器从 IOptions 读配置, 所以把生效后的选项写回选项管道
        context.Services.Configure<SnowMvcOptions>(options => CopyTo(effective, options));

        context.Services.AddScoped<SnowObjectValidationActionFilter>();

        // 关掉 [ApiController] 自带的 ModelState 过滤器, 统一由下面两个过滤器出 SnowApiResponse 格式的错误
        context.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        IMvcBuilder builder = context.Services.AddControllers(options =>
        {
            options.Filters.AddService<SnowObjectValidationActionFilter>(-1000);
            options.Filters.Add<SnowModelStateInvalidFilter>();
            options.Filters.Add<SnowResultFilter>();
            options.Filters.Add<SnowExceptionFilter>();
        });

        builder.AddMvcOptions(options =>
            options.Conventions.Add(new SnowApplicationServiceConvention(effective)));

        if (effective.AutoApiControllers)
        {
            RegisterApplicationServiceControllers(context, builder);
        }

        // 端点映射动作登记到统一挂载入口: 初始化时自动执行, 宿主不用写 UseSnowEndpoints()
        context.Services.AddSnowEndpoint(endpoints => endpoints.MapControllers());
    }

    private static void RegisterApplicationServiceControllers(ServiceConfigurationContext context, IMvcBuilder builder)
    {
        ApplicationPartManager partManager = context.Services.GetSingletonInstance<ApplicationPartManager>();
        partManager.FeatureProviders.Add(new SnowApplicationServiceControllerFeatureProvider());

        // 应用服务散落在各模块程序集里, 必须先把这些程序集登记成应用部件, 特性提供器才扫得到
        foreach (Assembly assembly in GetModuleAssemblies(context.Services))
        {
            bool exists = partManager.ApplicationParts
                .OfType<AssemblyPart>()
                .Any(part => part.Assembly == assembly);

            if (!exists)
            {
                partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            }
        }

        // 由容器激活 Controller: 应用服务是自己的类, 默认激活器只会找无参构造器
        builder.AddControllersAsServices();
    }

    private static IEnumerable<Assembly> GetModuleAssemblies(IServiceCollection services)
    {
        HashSet<Assembly> assemblies = [];

        IModuleContainer? container = services.GetSingletonInstanceOrNull<IModuleContainer>();
        if (container is not null)
        {
            foreach (SnowModuleDescriptor module in container.Modules)
            {
                assemblies.Add(module.Assembly);
            }
        }

        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            assemblies.Add(entryAssembly);
        }

        assemblies.Add(typeof(SnowMvcModule).Assembly);
        return assemblies;
    }

    private static SnowMvcOptions BuildOptions(IServiceCollection services)
    {
        SnowMvcOptions options = new();

        IConfigurationSection section = services.GetConfiguration().GetSection("Mvc");
        if (section.Exists())
        {
            section.Bind(options);
        }

        SnowMvcOptionsSetup? setup = services.GetSingletonInstanceOrNull<SnowMvcOptionsSetup>();
        setup?.Apply(options);

        return options;
    }

    private static void CopyTo(SnowMvcOptions source, SnowMvcOptions target)
    {
        target.RootPath = source.RootPath;
        target.AutoApiControllers = source.AutoApiControllers;
        target.WrapResultByDefault = source.WrapResultByDefault;
        target.IncludeExceptionMessage = source.IncludeExceptionMessage;
        target.AutoModelStateValidation = source.AutoModelStateValidation;
    }
}