using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Snow.DependencyInjection;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 把 <see cref="ISnowServiceConvention"/> 包成 MVC 应用模型约定(对应 ABP 的 <c>AbpServiceConventionWrapper</c>)。
/// </summary>
[DisableConventionalRegistration]
public class SnowServiceConventionWrapper : IApplicationModelConvention
{
    private readonly Lazy<ISnowServiceConvention> _convention;

    /// <summary>
    /// 构造器
    /// </summary>
    public SnowServiceConventionWrapper(IServiceCollection services)
    {
        _convention = services.GetRequiredServiceLazy<ISnowServiceConvention>();
    }

    /// <inheritdoc />
    public void Apply(ApplicationModel application)
    {
        _convention.Value.Apply(application);
    }
}