using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using Snow.Exceptions;
using Snow.Localization.Json;
using Snow.VirtualFileSystem;

namespace Snow.Localization.VirtualFiles.Json;

/// <summary>
/// 从虚拟路径加载 json 本地化文件: 目录下每个 *.json 的 <c>Culture</c> 字段决定文化名, 文本按 <c>__</c> 扁平化。
/// </summary>
internal static class JsonVirtualFileLocalizationLoader
{
    internal static void Load(
        string virtualPath,
        IVirtualFileProvider virtualFileProvider,
        Dictionary<string, Dictionary<string, LocalizedString>> target)
    {
        List<IFileInfo> files = virtualFileProvider
            .GetDirectoryContents(virtualPath)
            .Where(file => !file.IsDirectory && file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .ToList();

        foreach (IFileInfo file in files)
        {
            (string cultureName, Dictionary<string, LocalizedString> texts) = ReadFile(file);

            if (!target.TryGetValue(cultureName, out Dictionary<string, LocalizedString>? merged))
            {
                merged = new Dictionary<string, LocalizedString>(StringComparer.Ordinal);
                target[cultureName] = merged;
            }

            foreach (KeyValuePair<string, LocalizedString> pair in texts)
            {
                merged[pair.Key] = pair.Value;
            }
        }
    }

    private static (string CultureName, Dictionary<string, LocalizedString> Texts) ReadFile(IFileInfo file)
    {
        string json;

        try
        {
            using Stream stream = file.CreateReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            json = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new SnowException("本地化文件无法读取: " + file.Name, ex);
        }

        return JsonLocalizationDictionaryBuilder.BuildFromJsonString(json)
            ?? throw new SnowException("本地化文件缺少 culture: " + file.Name);
    }
}
