using Snow.Modularity;

namespace Snow.Json.Newtonsoft;

/// <summary>
/// Newtonsoft.Json 实现模块: 顶替 Snow.Json 自带的 System.Text.Json 实现
/// <para>装本包即以 ReplaceServices 顶替它; 两个实现按模块加载顺序, 后者胜。公共开关仍读 <c>Json</c> 节。</para>
/// </summary>
[DependsOn(typeof(SnowJsonModule))]
public sealed class SnowJsonNewtonsoftModule : SnowModule
{
}
