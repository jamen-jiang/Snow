using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Snow.DependencyInjection;

namespace Snow.Guids;

/* 这段实现取自 jhtodd/SequentialGuid: https://github.com/jhtodd/SequentialGuid/blob/master/SequentialGuid/Classes/SequentialGuid.cs */

/// <summary>
/// <see cref="IGuidGenerator"/> 的实现: 生成顺序 Guid; 用 <see cref="SnowSequentialGuidGeneratorOptions"/> 配置
/// </summary>
[TransientService]
public class SequentialGuidGenerator : IGuidGenerator
{
    /// <summary>
    /// 生成器选项
    /// </summary>
    public SnowSequentialGuidGeneratorOptions Options { get; }

    private static readonly RandomNumberGenerator RandomNumberGenerator = RandomNumberGenerator.Create();

    /// <summary>
    /// 构造顺序 Guid 生成器
    /// </summary>
    /// <param name="options">顺序 Guid 生成器选项</param>
    public SequentialGuidGenerator(IOptions<SnowSequentialGuidGeneratorOptions> options)
    {
        Options = options.Value;
    }

    /// <summary>
    /// 按选项里的默认类型生成一个顺序 Guid
    /// </summary>
    /// <returns>新的 Guid</returns>
    public Guid Create()
    {
        return Create(Options.GetDefaultSequentialGuidType());
    }

    /// <summary>
    /// 按指定类型生成一个顺序 Guid
    /// </summary>
    /// <param name="guidType">顺序 Guid 的类型, 决定有序部分放在哪里</param>
    /// <returns>新的 Guid</returns>
    public Guid Create(SequentialGuidType guidType)
    {
        // 先取 10 字节密码学强度的随机数据
        var randomBytes = new byte[10];
        RandomNumberGenerator.GetBytes(randomBytes);

        // 另一种做法: 用 Guid.NewGuid() 的字节当随机源 ——
        // byte[] randomBytes = Guid.NewGuid().ToByteArray();
        // 这比 RandomNumberGenerator 快, 但不建议: .NET 并不保证 Guid 数据的随机性,
        // 将来的版本(或 Mono 之类的其它实现)可能换用别的算法。

        // 随机基底有了, 接下来构造 6 字节的时间戳块。

        // 取从 DateTime.MinValue 起经过的毫秒数作为时间戳。
        // 比毫秒更细没有意义, 因为 DateTime.Now 本身的分辨率有限。

        // 48 位时间戳按毫秒计时, 大约 5900 年后才会溢出回绕, 多数场景够用 :)
        long timestamp = DateTime.UtcNow.Ticks / 10000L;

        // 转成字节
        byte[] timestampBytes = BitConverter.GetBytes(timestamp);

        // 由 Int64 转换而来, 小端系统上要反转一次
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestampBytes);
        }

        byte[] guidBytes = new byte[16];

        switch (guidType)
        {
            case SequentialGuidType.SequentialAsString:
            case SequentialGuidType.SequentialAsBinary:

                // 字符串版与字节数组版都是时间戳在前, 随机数据在后
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 0, 6);
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 6, 10);

                // 格式化成字符串时还要补偿一件事: .NET 把 Data1 块当 Int32、Data2 块当 Int16 看待,
                // 于是小端系统上这两段会被换序, 所以再反转回来
                if (guidType == SequentialGuidType.SequentialAsString && BitConverter.IsLittleEndian)
                {
                    Array.Reverse(guidBytes, 0, 4);
                    Array.Reverse(guidBytes, 4, 2);
                }

                break;

            case SequentialGuidType.SequentialAtEnd:

                // 末尾有序的版本反过来: 随机数据在前, 时间戳放在最后
                Buffer.BlockCopy(randomBytes, 0, guidBytes, 0, 10);
                Buffer.BlockCopy(timestampBytes, 2, guidBytes, 10, 6);
                break;
        }

        return new Guid(guidBytes);
    }
}
