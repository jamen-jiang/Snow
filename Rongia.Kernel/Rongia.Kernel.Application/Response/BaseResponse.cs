namespace Rongia.Kernel.Application.Response;

/// <summary>
/// 响应基类(Swagger 文档描述写在属性 XML 注释里,由框架启用 IncludeXmlComments 渲染)
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// 创建人Id(可空)
    /// </summary>
    public long CreatedBy { get; set; } = 0;

    /// <summary>
    /// 创建人
    /// </summary>
    public string CreatedByName { get; set; } = "";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新人Id(可空)
    /// </summary>
    public long? UpdatedBy { get; set; }

    /// <summary>
    /// 更新人(可空)
    /// </summary>
    public string? UpdatedByName { get; set; }

    /// <summary>
    /// 更新时间(可空)
    /// </summary>
    public DateTime? UpdatedTime { get; set; }
}
