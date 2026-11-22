namespace Snow.AspNetCore;

/// <summary>
/// 管道阶段: 模块只声明"挂在哪一段", 段内顺序就是登记顺序
/// <para>路由 / 认证 / 授权这三段由 <see cref="SnowAspNetCoreModule"/> 自己插在阶段之间, 模块因此不需要知道任何数字。</para>
/// </summary>
public enum SnowPipelineStage
{
    /// <summary>
    /// 最外层: 兜异常用, 排在路由、认证、多租户全部之前
    /// </summary>
    Outermost = 0,

    /// <summary>
    /// 路由之前: 链路标识、日志增强这类与路由无关的中间件
    /// </summary>
    BeforeRouting = 100,

    /// <summary>
    /// 路由之后、认证之前: 需要读端点元数据的中间件
    /// </summary>
    AfterRouting = 200,

    /// <summary>
    /// 认证之后、授权之前: 需要读已认证用户的中间件(多租户)
    /// </summary>
    AfterAuthentication = 300,

    /// <summary>
    /// 授权之后、端点之前: 需要读授权结果的中间件(限流)
    /// </summary>
    AfterAuthorization = 400,

    /// <summary>
    /// 端点之前: 自己处理请求的终端分支(仪表盘、接口文档)
    /// </summary>
    BeforeEndpoints = 500,
}
