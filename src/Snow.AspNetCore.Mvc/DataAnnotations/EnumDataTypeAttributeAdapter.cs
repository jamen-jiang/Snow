using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using Snow;

namespace Snow.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// <see cref="EnumDataTypeAttribute"/> 的 MVC 适配器, 仅用于服务端错误消息本地化。
/// </summary>
public class EnumDataTypeAttributeAdapter : AttributeAdapterBase<EnumDataTypeAttribute>
{
    public EnumDataTypeAttributeAdapter(
        EnumDataTypeAttribute attribute,
        IStringLocalizer? stringLocalizer)
        : base(attribute, stringLocalizer)
    {
    }

    public override void AddValidation(ClientModelValidationContext context)
    {
        Check.NotNull(context, nameof(context));

        // 枚举没有内置的客户端校验规则; 该适配器只用于服务端错误消息本地化。
    }

    public override string GetErrorMessage(ModelValidationContextBase validationContext)
    {
        Check.NotNull(validationContext, nameof(validationContext));

        return GetErrorMessage(
            validationContext.ModelMetadata,
            validationContext.ModelMetadata.GetDisplayName()
        );
    }
}
