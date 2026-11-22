using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Snow.Json.Newtonsoft;

/// <summary>
/// Newtonsoft.Json 选项: 默认值尽量与框架的 System.Text.Json 约定对齐(小驼峰、忽略 null、枚举小驼峰字符串)
/// <para>公共开关(缩进/忽略 null)在 <c>Json</c> 配置节的 SnowJsonOptions 里, 这里只管库特有细节。</para>
/// </summary>
public sealed class SnowNewtonsoftJsonOptions
{
    /// <summary>
    /// 构造器: 建好带默认值的序列化设置
    /// </summary>
    public SnowNewtonsoftJsonOptions()
    {
        Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Culture = CultureInfo.InvariantCulture,
        };
        Settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
    }

    /// <summary>
    /// 序列化设置: 直接加 Converter 或换 ContractResolver
    /// </summary>
    public JsonSerializerSettings Settings { get; }
}
