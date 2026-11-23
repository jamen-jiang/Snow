using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

/// <summary>
/// 针对 <see cref="Stream"/> 的扩展方法: 把流内容整块读出来或整块拷进内存流。
/// </summary>
public static class SnowStreamExtensions
{
    /// <summary>
    /// 一次读完整个流的内容; 能定位的流会先回到开头。
    /// </summary>
    /// <param name="stream">目标流</param>
    /// <returns>流里的全部字节</returns>
    public static byte[] GetAllBytes(this Stream stream)
    {
        if (stream is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using (var ms = stream.CreateMemoryStream())
        {
            return ms.ToArray();
        }
    }

    /// <summary>
    /// 异步读完整个流的内容; 能定位的流会先回到开头。
    /// </summary>
    /// <param name="stream">目标流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流里的全部字节</returns>
    public static async Task<byte[]> GetAllBytesAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using (var ms = await stream.CreateMemoryStreamAsync(cancellationToken))
        {
            return ms.ToArray();
        }
    }

    /// <summary>
    /// 从流开头开始异步拷贝到目标流(能定位的流会先回到开头), 并把取消令牌一起传下去。
    /// </summary>
    /// <param name="stream">源流</param>
    /// <param name="destination">目标流</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static Task CopyToAsync(this Stream stream, Stream destination, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
        
        return stream.CopyToAsync(
            destination,
            81920, //这是默认缓冲区大小, 显式写出来只是为了能传 cancellationToken
            cancellationToken
        );
    }
    
    /// <summary>
    /// 把流内容异步拷进一个新 <see cref="MemoryStream"/> 并返回, 返回前两边都定位到开头。
    /// </summary>
    /// <param name="stream">源流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>装好内容的 <see cref="MemoryStream"/>, 位置在 0</returns>
    public async static Task<MemoryStream> CreateMemoryStreamAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
        
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
        
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// 把流内容同步拷进一个新 <see cref="MemoryStream"/> 并返回, 返回前两边都定位到开头。
    /// </summary>
    /// <param name="stream">源流</param>
    /// <returns>装好内容的 <see cref="MemoryStream"/>, 位置在 0</returns>
    public static MemoryStream CreateMemoryStream(this Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
        
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
        
        memoryStream.Position = 0;
        return memoryStream;
    }
}
