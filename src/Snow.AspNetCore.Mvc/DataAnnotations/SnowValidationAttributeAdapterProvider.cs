using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Snow.Validation;

namespace Snow.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// 动态校验特性与枚举特性的 MVC 适配器提供器, 对应 ABP 的 <c>AbpValidationAttributeAdapterProvider</c>。
/// </summary>
public class SnowValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
{
    private readonly ValidationAttributeAdapterProvider _defaultAdapter;

    public SnowValidationAttributeAdapterProvider(ValidationAttributeAdapterProvider defaultAdapter)
    {
        _defaultAdapter = defaultAdapter;
    }

    /// <inheritdoc />
    public virtual IAttributeAdapter? GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? stringLocalizer)
    {
        var type = attribute.GetType();

        if (type == typeof(DynamicStringLengthAttribute))
        {
            return new DynamicStringLengthAttributeAdapter((DynamicStringLengthAttribute)attribute, stringLocalizer);
        }

        if (type == typeof(DynamicMaxLengthAttribute))
        {
            return new DynamicMaxLengthAttributeAdapter((DynamicMaxLengthAttribute)attribute, stringLocalizer);
        }

        if (type == typeof(DynamicRangeAttribute))
        {
            return new DynamicRangeAttributeAdapter((DynamicRangeAttribute)attribute, stringLocalizer);
        }

        if (type == typeof(EnumDataTypeAttribute))
        {
            return new EnumDataTypeAttributeAdapter((EnumDataTypeAttribute)attribute, stringLocalizer);
        }

        return _defaultAdapter.GetAttributeAdapter(attribute, stringLocalizer);
    }
}
