using System.Diagnostics;

namespace System.Reflection;

/// <summary>
/// 针对 <see cref="Assembly"/> 的扩展方法。
/// </summary>
public static class SnowAssemblyExtensions
{
    /// <summary>
    /// 取程序集所在文件里记录的版本号(文件版本, 不是程序集版本)。
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <returns>文件版本字符串</returns>
    public static string GetFileVersion(this Assembly assembly)
    {
        return FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion!;
    }
}
