using Snow.Modularity;

namespace Snow.AspNetCore;

/// <summary>
/// 管道运行状态: 记录阶段是否已被应用、中间件是否已挂过
/// <para>作用有两个: ①同一阶段被应用两次直接报错; ②同一中间件被"手动 Use"与"管道展开"各挂一次时只生效一次。</para>
/// </summary>
public sealed class SnowPipelineState
{
    private readonly HashSet<string> _appliedKeys = new(StringComparer.Ordinal);
    private readonly HashSet<SnowPipelineStage> _appliedStages = [];

    /// <summary>
    /// 标记某个中间件已挂载, 首次返回 true, 重复返回 false
    /// </summary>
    /// <param name="key">中间件去重键</param>
    /// <returns>本次是否首次标记</returns>
    public bool TryMarkApplied(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _appliedKeys.Add(key);
    }

    /// <summary>
    /// 判断某个中间件是否已挂载
    /// </summary>
    /// <param name="key">中间件去重键</param>
    /// <returns>已挂载返回 true</returns>
    public bool IsApplied(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _appliedKeys.Contains(key);
    }

    /// <summary>
    /// 标记阶段已应用, 重复应用抛异常
    /// </summary>
    /// <param name="stage">阶段</param>
    /// <exception cref="SnowInitializationException">同一阶段被应用了两次</exception>
    public void MarkStageApplied(SnowPipelineStage stage)
    {
        if (!_appliedStages.Add(stage))
        {
            throw new SnowInitializationException(
                "中间件管道阶段 " + stage + " 被应用了两次: 每个阶段只应调用一次 UseSnowPipeline 或 UseSnowStage。");
        }
    }

    /// <summary>
    /// 判断阶段是否已应用
    /// </summary>
    /// <param name="stage">阶段</param>
    /// <returns>已应用返回 true</returns>
    public bool IsStageApplied(SnowPipelineStage stage)
    {
        return _appliedStages.Contains(stage);
    }

    /// <summary>
    /// 已应用的阶段
    /// </summary>
    public IReadOnlyCollection<SnowPipelineStage> AppliedStages => _appliedStages;
}
