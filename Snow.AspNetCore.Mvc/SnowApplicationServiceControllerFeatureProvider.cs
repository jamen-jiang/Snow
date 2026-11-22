using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Snow.Application.Services;

namespace Snow.AspNetCore.Mvc;

/// <summary>
/// 把实现了 <see cref="IApplicationService"/> 的类注册成 Controller 的特性提供器(对应 ABP 的 AbpConventionalControllerFeatureProvider)
/// <para>MVC 默认只认名字以 Controller 结尾或带 [Controller] 的类, 应用服务两者都不是, 所以必须在这里补登记。</para>
/// <para>直接实现特性提供器接口而不继承 <c>ControllerFeatureProvider</c>: 后者在 .NET 11 里已不可重写, 且默认约定由框架自己那份负责。</para>
/// </summary>
public sealed class SnowApplicationServiceControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    /// <inheritdoc />
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(feature);

        foreach (ApplicationPart part in parts)
        {
            if (part is not AssemblyPart assemblyPart)
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assemblyPart.Assembly.GetExportedTypes();
            }
            catch (Exception exception) when (exception is NotSupportedException or FileNotFoundException)
            {
                continue;
            }

            foreach (Type type in types)
            {
                if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                if (!typeof(IApplicationService).IsAssignableFrom(type))
                {
                    continue;
                }

                TypeInfo typeInfo = type.GetTypeInfo();
                if (!feature.Controllers.Contains(typeInfo))
                {
                    feature.Controllers.Add(typeInfo);
                }
            }
        }
    }
}
