using Microsoft.Extensions.Logging;

namespace Snow.Logging;

/// <summary>
/// 带 <see cref="LogLevel"/> 属性的接口(见 <see cref="LogLevel"/>)。
/// </summary>
public interface IHasLogLevel
{
    /// <summary>
    /// 日志级别。
    /// </summary>
    LogLevel LogLevel { get; set; }
}
