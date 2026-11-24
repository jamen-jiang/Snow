namespace Snow.OpenApi;

/// <summary>
/// OpenAPI 文档选项
/// </summary>
public sealed class SnowOpenApiOptions
{
    /// <summary>
    /// 是否启用文档, 关闭后模块只登记服务不挂端点
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 文档标题
    /// </summary>
    public string Title { get; set; } = "Snow API";

    /// <summary>
    /// 文档版本(同时作为文档名与路由段)
    /// </summary>
    public string Version { get; set; } = "v1";

    /// <summary>
    /// 文档描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 界面与文档的路由前缀, 默认 "openapi"
    /// </summary>
    public string RoutePrefix { get; set; } = "openapi";

    /// <summary>
    /// 是否加载各程序集旁的 XML 注释文件(前提是项目打开了 GenerateDocumentationFile)
    /// </summary>
    public bool IncludeXmlComments { get; set; } = true;

    /// <summary>
    /// 是否登记 Bearer 令牌的授权方式, 便于在界面上带 JWT 调试
    /// </summary>
    public bool AddJwtBearerSecurity { get; set; } = true;
}
