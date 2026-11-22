using Microsoft.AspNetCore.Builder;

namespace Snow.AspNetCore;

/// <summary>
/// 管道步骤契约: 一个"登记器"对象, 在锚点被展开时把自己的中间件装进管道
/// <para>适合包内需要组合多个中间件、或需要读选项后再决定挂载方式的场景;
/// 只挂一个中间件时直接用 <c>AddSnowMiddleware&lt;TMiddleware&gt;(阶段, 顺序)</c> 即可。</para>
/// </summary>
public interface ISnowMiddlewareStep
{
    /// <summary>
    /// 往管道里装中间件
    /// </summary>
    /// <param name="app">应用构建器</param>
    void Configure(IApplicationBuilder app);
}
