namespace Snow.Settings;

/// <summary>
/// 配置项定义的声明源(对应 ABP 的 SettingDefinitionProvider): 子类在 <see cref="Define"/> 里登记自己的配置项
/// <para>在模块里用 <c>Configure&lt;SnowSettingOptions&gt;(o =&gt; o.DefinitionProviders.Add&lt;MyProvider&gt;())</c> 挂上去。</para>
/// </summary>
public abstract class SettingDefinitionProvider
{
    /// <summary>
    /// 登记配置项
    /// </summary>
    /// <param name="context">登记上下文</param>
    public abstract void Define(ISettingDefinitionContext context);
}
