namespace Rongia.Application.Port.Template;

/// <summary>
/// 模板渲染端口(实现由基础设施层提供,如 Thymeleaf → Scriban/Razor)
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// 渲染模板
    /// </summary>
    /// <param name="template">模板字符串</param>
    /// <param name="variables">变量</param>
    /// <returns>渲染结果</returns>
    string Render(string template, IReadOnlyDictionary<string, object?> variables);
}
