using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snow.AspNetCore.Mvc.DataAnnotations;
using Snow.AspNetCore.Mvc.Json;
using Snow.Json;
using Snow.Modularity;
using Snow.Validation;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// MVC 与自动 API 模块
/// <para>对应 ABP 的 <c>AbpAspNetCoreMvcModule</c>。三件事: 给远程服务控制器按约定算路由与动词(应用模型约定)、
/// 统一异常响应(全局过滤器)、统一参数与模型校验。</para>
/// <para>依赖 <see cref="SnowAspNetCoreModule"/>: 自动 API 跑在它提供的 HTTP 上下文之上。</para>
/// <para>依赖 <see cref="SnowValidationModule"/>: 自动校验过滤器用 <see cref="ISnowObjectValidator"/> 跑 DataAnnotations 与 FluentValidation,
/// 与 ABP 把 <c>AbpValidationActionFilter</c> 放在 Volo.Abp.AspNetCore.Mvc 里同形 —— 校验只有一个包, 不需要 AspNetCore 后缀的校验包。</para>
/// <para>端点与 ABP 一样由模块声明(AbpEndpointRouterOptions.EndpointConfigureActions 同形的 SnowEndpointRouterOptions),
/// 框架在全部模块初始化完成之后统一映射, 宿主不用写 <c>MapControllers()</c>。</para>
/// </summary>
[DependsOn(typeof(SnowAspNetCoreModule), typeof(SnowValidationModule), typeof(SnowJsonModule))]
public sealed class SnowAspNetCoreMvcModule : SnowModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Services.Configure<SnowAspNetCoreMvcOptions>(options => context.Configuration.GetSection("Mvc").Bind(options));

        // 框架过滤器统一用 [TransientService] 注解自动登记; SnowMvcOptionsExtensions 里只生成 ServiceFilterAttribute 从容器取实例

        // 关掉 [ApiController] 自带的 ModelState 过滤器, 统一由 SnowValidationActionFilter 处理模型校验
        context.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        // 对应 ABP: 先建 MVC Core, 再统一应用 SnowMvcOptionsExtensions.AddSnow
        var mvcCoreBuilder = context.Services.AddMvcCore();
        context.Services.ExecutePreConfiguredActions(mvcCoreBuilder);
        mvcCoreBuilder.AddSnowJson();

        IMvcBuilder builder = context.Services.AddControllers();
        context.Services.AddSnowOptions<MvcOptions>()
            .Configure<IServiceProvider>((mvcOptions, serviceProvider) =>
            {
                mvcOptions.AddSnow(context.Services);
            });

        // DataAnnotations 适配器: 对应 ABP 的 AbpValidationAttributeAdapterProvider, 支持动态长度/范围与枚举本地化
        context.Services.Replace(ServiceDescriptor.Singleton<IValidationAttributeAdapterProvider, SnowValidationAttributeAdapterProvider>());
        context.Services.AddSingleton<ValidationAttributeAdapterProvider>();

        // 动作描述符定型后摘掉框架过滤器, 对应 ABP 的 AbpMvcActionDescriptorProvider
        context.Services.TryAddEnumerable(ServiceDescriptor.Transient<IActionDescriptorProvider, SnowMvcActionDescriptorProvider>());

        // 端点映射交给统一挂载: 与 ABP 的 EndpointConfigureActions 同形
        Configure<SnowEndpointRouterOptions>(options =>
            options.EndpointConfigureActions.Add(endpointContext => endpointContext.Endpoints.MapControllers()));
    }
}
