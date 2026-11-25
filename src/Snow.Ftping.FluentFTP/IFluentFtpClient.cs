using global::FluentFTP;

namespace Snow.Ftping.FluentFTP;

/// <summary>
/// 暴露底层 FluentFTP 异步连接的客户端
/// <para>给需要自己控制连接(一次会话里连做多件事、或用 FluentFTP 独有 API)的场景用;
/// 只做一次上传下载直接用 <see cref="IFtpClient"/> 即可。</para>
/// </summary>
public interface IFluentFtpClient : IFtpClient
{
    /// <summary>
    /// 按当前配置项建一个尚未连接的 FluentFTP 异步客户端
    /// </summary>
    /// <param name="cancellationToken">取消令牌(读取配置项时使用)</param>
    /// <returns>客户端实例, <b>调用方负责连接与释放</b></returns>
    Task<AsyncFtpClient> BuildClientAsync(CancellationToken cancellationToken = default);
}
