using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Snow.Authorization;
using Snow.Data;
using Snow.DependencyInjection;
using Snow.Domain.Entities;
using Snow.ExceptionHandling;
using Snow.ExceptionHandling.Localization;
using Snow.Exceptions;
using Snow.Http;
using Snow.Localization;
using Snow.Validation;

namespace Snow.AspNetCore.ExceptionHandling;

/// <summary>
/// 默认的异常 → 远端错误信息转换器(逐方法对齐 ABP 11 的 <c>DefaultExceptionToErrorInfoConverter</c>)
/// </summary>
[TransientService]
public class DefaultExceptionToErrorInfoConverter : IExceptionToErrorInfoConverter
{
    /// <summary>
    /// 异常处理选项
    /// </summary>
    protected SnowExceptionHandlingOptions ExceptionHandlingOptions { get; }

    /// <summary>
    /// 异常本地化选项
    /// </summary>
    protected SnowExceptionLocalizationOptions LocalizationOptions { get; }

    /// <summary>
    /// 字符串本地化器工厂
    /// </summary>
    protected IStringLocalizerFactory StringLocalizerFactory { get; }

    /// <summary>
    /// 异常处理内置文案的本地化器
    /// </summary>
    protected IStringLocalizer<SnowExceptionHandlingResource> L { get; }

    /// <summary>
    /// 服务提供器
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="exceptionHandlingOptions">异常处理选项</param>
    /// <param name="localizationOptions">异常本地化选项</param>
    /// <param name="stringLocalizerFactory">字符串本地化器工厂</param>
    /// <param name="stringLocalizer">异常处理本地化器</param>
    /// <param name="serviceProvider">服务提供器</param>
    public DefaultExceptionToErrorInfoConverter(
        IOptions<SnowExceptionHandlingOptions> exceptionHandlingOptions,
        IOptions<SnowExceptionLocalizationOptions> localizationOptions,
        IStringLocalizerFactory stringLocalizerFactory,
        IStringLocalizer<SnowExceptionHandlingResource> stringLocalizer,
        IServiceProvider serviceProvider)
    {
        ExceptionHandlingOptions = exceptionHandlingOptions.Value;
        ServiceProvider = serviceProvider;
        StringLocalizerFactory = stringLocalizerFactory;
        L = stringLocalizer;
        LocalizationOptions = localizationOptions.Value;
    }

    /// <inheritdoc />
    /// <param name="exception">异常</param>
    /// <param name="optionsAction">选项配置回调, 可为 null</param>
    public virtual RemoteServiceErrorInfo Convert(Exception exception, Action<SnowExceptionHandlingOptions>? optionsAction = null)
    {
        SnowExceptionHandlingOptions options = CreateDefaultOptions();
        optionsAction?.Invoke(options);

        RemoteServiceErrorInfo errorInfo = CreateErrorInfoWithoutCode(exception, options);

        if (exception is IHasErrorCode hasErrorCode)
        {
            errorInfo.Code = hasErrorCode.Code;
        }

        return errorInfo;
    }

    /// <summary>
    /// 生成不含错误码的错误信息(错误码在转换入口统一补齐)
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="options">本次生效的选项</param>
    protected virtual RemoteServiceErrorInfo CreateErrorInfoWithoutCode(Exception exception, SnowExceptionHandlingOptions options)
    {
        if (options.SendExceptionsDetailsToClients)
        {
            return CreateDetailedErrorInfoFromException(exception, options.SendStackTraceToClients);
        }

        exception = TryToGetActualException(exception);

        if (exception is SnowRemoteCallException { Error: not null } remoteCallException)
        {
            RemoteServiceErrorInfo remoteErrorInfo = remoteCallException.Error;

            if (remoteErrorInfo.Message == SnowExceptionHandlingConsts.Unauthorized)
            {
                remoteErrorInfo.Message = L[SnowExceptionHandlingConsts.Unauthorized];
            }

            if (remoteErrorInfo.Details == SnowExceptionHandlingConsts.SessionExpired)
            {
                remoteErrorInfo.Details = L[SnowExceptionHandlingConsts.SessionExpired];
            }

            return remoteErrorInfo;
        }

        if (exception is SnowDbConcurrencyException)
        {
            return new RemoteServiceErrorInfo(L["SnowDbConcurrencyErrorMessage"]);
        }

        if (exception is EntityNotFoundException entityNotFoundException)
        {
            return CreateEntityNotFoundError(entityNotFoundException);
        }

        var errorInfo = new RemoteServiceErrorInfo();

        if (exception is IUserFriendlyException || exception is SnowRemoteCallException)
        {
            errorInfo.Message = exception.Message;
            errorInfo.Details = (exception as IHasErrorDetails)?.Details;
        }

        if (exception is IHasValidationErrors validationException)
        {
            if (errorInfo.Message.IsNullOrEmpty())
            {
                errorInfo.Message = L["ValidationErrorMessage"];
            }

            if (errorInfo.Details.IsNullOrEmpty())
            {
                errorInfo.Details = GetValidationErrorNarrative(validationException);
            }

            errorInfo.ValidationErrors = GetValidationErrorInfos(validationException);
        }

        TryToLocalizeExceptionMessage(exception, errorInfo);

        if (errorInfo.Message.IsNullOrEmpty())
        {
            errorInfo.Message = L["InternalServerErrorMessage"];
        }

        if (options.SendExceptionDataToClientTypes.Any(type => type.IsInstanceOfType(exception)))
        {
            errorInfo.Data = exception.Data;
        }

        return errorInfo;
    }

    /// <summary>
    /// 尝试本地化异常消息(对应 ABP: 支持 <see cref="ILocalizeErrorMessage"/> 与错误码命名空间映射)
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="errorInfo">已构造的错误信息</param>
    protected virtual void TryToLocalizeExceptionMessage(Exception exception, RemoteServiceErrorInfo errorInfo)
    {
        if (exception is ILocalizeErrorMessage localizeErrorMessage)
        {
            using IServiceScope serviceScope = ServiceProvider.CreateScope();
            errorInfo.Message = localizeErrorMessage.LocalizeMessage(new LocalizationContext(serviceScope.ServiceProvider));
            return;
        }

        if (exception is not IHasErrorCode hasErrorCode)
        {
            return;
        }

        string? code = hasErrorCode.Code;
        if (code.IsNullOrWhiteSpace() || !code.Contains(':'))
        {
            return;
        }

        string errorCodeNamespace = code.Split(':')[0];
        Type? resourceType = LocalizationOptions.ErrorCodeNamespaceMappings.GetOrDefault(errorCodeNamespace);
        if (resourceType == null)
        {
            return;
        }

        LocalizedString localizedString = StringLocalizerFactory.Create(resourceType)[code];
        if (localizedString.ResourceNotFound)
        {
            return;
        }

        string message = localizedString.Value;
        if (exception.Data != null && exception.Data.Count > 0)
        {
            foreach (object key in exception.Data.Keys)
            {
                message = message.Replace("{" + key + "}", exception.Data[key!]?.ToString() ?? string.Empty);
            }
        }

        errorInfo.Message = message;
    }

    /// <summary>
    /// 构造"实体不存在"的错误信息(对齐 ABP: 优先用本地化模板填充实体类型与主键)
    /// </summary>
    /// <param name="exception">实体不存在异常</param>
    protected virtual RemoteServiceErrorInfo CreateEntityNotFoundError(EntityNotFoundException exception)
    {
        if (exception.EntityType != null)
        {
            return new RemoteServiceErrorInfo(
                exception.Id != null
                    ? string.Format(L["EntityNotFoundErrorMessage"], exception.EntityType.Name, exception.Id)
                    : string.Format(L["EntityNotFoundErrorMessageWithoutId"], exception.EntityType.Name));
        }

        return new RemoteServiceErrorInfo(exception.Message);
    }

    /// <summary>
    /// 尝试取出真正想表达的异常: 单层 <see cref="AggregateException"/> 包裹常见业务/校验异常时展开
    /// </summary>
    /// <param name="exception">原始异常</param>
    protected virtual Exception TryToGetActualException(Exception exception)
    {
        if (exception is AggregateException { InnerException: not null } aggregateException &&
            (aggregateException.InnerException is SnowValidationException ||
             aggregateException.InnerException is SnowAuthorizationException ||
             aggregateException.InnerException is EntityNotFoundException ||
             aggregateException.InnerException is IBusinessException))
        {
            return aggregateException.InnerException;
        }

        return exception;
    }

    /// <summary>
    /// 生成"包含细节"的错误信息: 逐层拼接异常链、校验叙述与可选堆栈
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="sendStackTraceToClients">是否把堆栈一并返回</param>
    protected virtual RemoteServiceErrorInfo CreateDetailedErrorInfoFromException(Exception exception, bool sendStackTraceToClients)
    {
        var detailBuilder = new StringBuilder();
        AddExceptionToDetails(exception, detailBuilder, sendStackTraceToClients);

        var errorInfo = new RemoteServiceErrorInfo(exception.Message, detailBuilder.ToString(), null, exception.Data);

        if (exception is SnowValidationException validationException)
        {
            errorInfo.ValidationErrors = GetValidationErrorInfos(validationException);
        }

        TryToLocalizeExceptionMessage(exception, errorInfo);

        return errorInfo;
    }

    /// <summary>
    /// 把一条异常(含内部异常与聚合异常)逐层追加到细节文本
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="detailBuilder">细节文本构造器</param>
    /// <param name="sendStackTraceToClients">是否把堆栈一并返回</param>
    protected virtual void AddExceptionToDetails(Exception exception, StringBuilder detailBuilder, bool sendStackTraceToClients)
    {
        detailBuilder.AppendLine(exception.GetType().Name + ": " + exception.Message);

        if (exception is IUserFriendlyException && exception is IHasErrorDetails hasErrorDetails)
        {
            string? details = hasErrorDetails.Details;
            if (!details.IsNullOrEmpty())
            {
                detailBuilder.AppendLine(details);
            }
        }

        if (exception is SnowValidationException validationException && validationException.ValidationErrors.Count > 0)
        {
            detailBuilder.AppendLine(GetValidationErrorNarrative(validationException));
        }

        if (sendStackTraceToClients && !exception.StackTrace.IsNullOrEmpty())
        {
            detailBuilder.AppendLine("STACK TRACE: " + exception.StackTrace);
        }

        if (exception.InnerException != null)
        {
            AddExceptionToDetails(exception.InnerException, detailBuilder, sendStackTraceToClients);
        }

        if (exception is AggregateException aggregateException && !aggregateException.InnerExceptions.IsNullOrEmpty())
        {
            foreach (Exception innerException in aggregateException.InnerExceptions)
            {
                AddExceptionToDetails(innerException, detailBuilder, sendStackTraceToClients);
            }
        }
    }

    /// <summary>
    /// 把校验错误列表整理为 <see cref="RemoteServiceValidationErrorInfo"/> 数组(成员名按 ABP 习惯转 camelCase)
    /// </summary>
    /// <param name="validationException">带校验错误的异常</param>
    protected virtual RemoteServiceValidationErrorInfo[] GetValidationErrorInfos(IHasValidationErrors validationException)
    {
        var validationErrors = new List<RemoteServiceValidationErrorInfo>();

        foreach (ValidationResult validationError in validationException.ValidationErrors)
        {
            var errorInfo = new RemoteServiceValidationErrorInfo(validationError.ErrorMessage!);
            if (validationError.MemberNames != null && validationError.MemberNames.Any())
            {
                errorInfo.Members = validationError.MemberNames.Select(member => member.ToCamelCase(false, false)).ToArray();
            }

            validationErrors.Add(errorInfo);
        }

        return validationErrors.ToArray();
    }

    /// <summary>
    /// 把校验错误列表拼成一段叙述文本
    /// </summary>
    /// <param name="validationException">带校验错误的异常</param>
    protected virtual string GetValidationErrorNarrative(IHasValidationErrors validationException)
    {
        var detailBuilder = new StringBuilder();
        detailBuilder.AppendLine(L["ValidationNarrativeErrorMessageTitle"]);

        foreach (ValidationResult validationError in validationException.ValidationErrors)
        {
            detailBuilder.AppendFormat(" - {0}", validationError.ErrorMessage);
            detailBuilder.AppendLine();
        }

        return detailBuilder.ToString();
    }

    /// <summary>
    /// 由全局配置生成一份本次转换用的临时选项副本(与 ABP 一致: 只复制两个布尔开关)
    /// </summary>
    protected virtual SnowExceptionHandlingOptions CreateDefaultOptions()
    {
        return new SnowExceptionHandlingOptions
        {
            SendExceptionsDetailsToClients = ExceptionHandlingOptions.SendExceptionsDetailsToClients,
            SendStackTraceToClients = ExceptionHandlingOptions.SendStackTraceToClients,
        };
    }
}
