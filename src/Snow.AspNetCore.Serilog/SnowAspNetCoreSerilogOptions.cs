namespace Snow.AspNetCore.Serilog;

/// <summary>
/// Serilog 增强选项: 决定当前上下文相关字段以什么属性名写进日志
/// <para>对齐 ABP 的 <c>AbpAspNetCoreSerilogOptions</c>: 只提供默认属性名, 使用方可在注册时改写。</para>
/// </summary>
public class SnowAspNetCoreSerilogOptions
{
    /// <summary>
    /// 各字段写入日志上下文时使用的属性名
    /// </summary>
    public AllEnricherPropertyNames EnricherPropertyNames { get; } = new AllEnricherPropertyNames();

    /// <summary>
    /// 日志上下文属性名集合
    /// </summary>
    public class AllEnricherPropertyNames
    {
        /// <summary>
        /// 用户Id。默认 "UserId"
        /// </summary>
        public string UserId { get; set; } = "UserId";

        /// <summary>
        /// 链路Id。默认 "CorrelationId"
        /// </summary>
        public string CorrelationId { get; set; } = "CorrelationId";
    }
}
