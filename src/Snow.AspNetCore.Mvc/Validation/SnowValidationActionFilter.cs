using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Snow.AspNetCore.Filters;
using Snow.DependencyInjection;
using Snow.Reflection;
using Snow.Validation;

namespace Snow.AspNetCore.Mvc.Validation;

/// <summary>
/// MVC 自动校验过滤器(对齐 ABP 的 <c>AbpValidationActionFilter</c>): 进 Action 之前校验模型状态与每个复杂参数, 有错误抛 <see cref="SnowValidationException"/> 由异常处理翻译成 400 与字段错误列表
/// <para>位置与命名对齐 ABP: 放在 <c>Validation</c> 子目录, 命名空间 <c>Snow.AspNetCore.Mvc.Validation</c>,
/// 与 <c>Volo.Abp.AspNetCore.Mvc.Validation.AbpValidationActionFilter</c> 同构。</para>
/// <para>控制流与 ABP 逐段对应: <c>IsControllerAction &amp;&amp; HasObjectResult</c> 门控 → AutoModelValidation 开关 →
/// 三处 <see cref="DisableValidationAttribute"/> 判断(方法 / 控制器类型 / 改写后的方法) → <see cref="IModelStateValidator"/> 校验模型状态 →
/// <c>context.Controller is IValidationEnabled</c> 时 <see cref="ValidateActionArgumentsAsync"/> 校验参数(有错抛异常) → next。</para>
/// <para>与 ABP 的差异(Snow 侧基建不同所致): 都用 <c>IMethodInvocationValidator</c> 校验方法参数, 区别在底层用 <see cref="ISnowObjectValidator"/>
/// 聚合 <see cref="ValidationResult"/> 后抛 <see cref="SnowValidationException"/>(ABP 抛 <c>AbpValidationException</c>), 异常信封与状态码由异常处理统一产出。</para>
/// <para>排在管线最前(Order = -1000), 使校验失败先于业务代码与结果包装。</para>
/// </summary>
[TransientService]
public class SnowValidationActionFilter : IAsyncActionFilter, ISnowFilter
{
    /// <inheritdoc />
    /// <param name="context">上下文</param>
    /// <param name="next">后续执行委托</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!context.ActionDescriptor.IsControllerAction() ||
            !context.ActionDescriptor.HasObjectResult())
        {
            await next();
            return;
        }

        if (!context.GetRequiredService<IOptions<SnowAspNetCoreMvcOptions>>().Value.AutoModelValidation)
        {
            await next();
            return;
        }

        if (ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<DisableValidationAttribute>(context.ActionDescriptor.GetMethodInfo()) != null)
        {
            await next();
            return;
        }

        if (ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<DisableValidationAttribute>(context.Controller.GetType()) != null)
        {
            await next();
            return;
        }

        MethodInfo? effectiveMethod = GetEffectiveMethodInfo(context);
        if (effectiveMethod != null)
        {
            if (ReflectionHelper.GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<DisableValidationAttribute>(effectiveMethod) != null)
            {
                await next();
                return;
            }
        }

        context.GetRequiredService<IModelStateValidator>().Validate(context.ModelState);

        if (context.Controller is IValidationEnabled)
        {
            await ValidateActionArgumentsAsync(context, effectiveMethod);
        }

        await next();
    }

    /// <summary>
    /// 解析真正要校验的方法(子类重写基类动作方法时取重写版本), 对应 ABP 的 <c>GetEffectiveMethodInfo</c>
    /// </summary>
    /// <param name="context">动作执行上下文</param>
    /// <returns>重写方法信息; 没有重写返回 null</returns>
    protected virtual MethodInfo? GetEffectiveMethodInfo(ActionExecutingContext context)
    {
        MethodInfo baseMethod = context.ActionDescriptor.GetMethodInfo();
        if (baseMethod.DeclaringType == context.Controller.GetType())
        {
            return null;
        }

        return context.Controller.GetType().GetMethods().FirstOrDefault(x =>
            x.DeclaringType == context.Controller.GetType() &&
            x.Name == baseMethod.Name &&
            x.ReturnType == baseMethod.ReturnType &&
            x.GetParameters().Select(p => p.ToString()).SequenceEqual(baseMethod.GetParameters().Select(p => p.ToString())));
    }

    /// <summary>
    /// 校验 Action 参数中的复杂对象, 对应 ABP 的 <c>ValidateActionArgumentsAsync</c>; 有错误时抛 <see cref="SnowValidationException"/> 中断执行
    /// </summary>
    /// <param name="context">动作执行上下文</param>
    /// <param name="effectiveMethod">解析后的方法(可为 null, 回退到描述符方法)</param>
    protected virtual async Task ValidateActionArgumentsAsync(ActionExecutingContext context, MethodInfo? effectiveMethod = null)
    {
        var methodInfo = effectiveMethod ?? context.ActionDescriptor.GetMethodInfo();

        var parameterValues = methodInfo.GetParameters()
            .Select(p => context.ActionArguments.TryGetValue(p.Name!, out var value) ? value : null)
            .ToArray();

        await context.GetRequiredService<IMethodInvocationValidator>().ValidateAsync(
            new MethodInvocationValidationContext(
                context.Controller,
                methodInfo,
                parameterValues
            )
        );
    }
}
