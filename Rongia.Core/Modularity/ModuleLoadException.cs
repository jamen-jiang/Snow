using Microsoft.Extensions.Logging;

namespace Rongia.Modularity;

/// <summary>
/// 模块加载失败时抛出的异常:类型不是模块、模块抽象、依赖类型非法或依赖成环
/// </summary>
public class ModuleLoadException : Exception
{
    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    public ModuleLoadException(string message)
        : base(message) { }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public ModuleLoadException(string message, Exception innerException)
        : base(message, innerException) { }
}
