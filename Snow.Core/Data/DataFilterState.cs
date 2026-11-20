namespace Snow.Data;

/// <summary>
/// 过滤器状态值
/// </summary>
public class DataFilterState
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="isEnabled">是否启用</param>
    public DataFilterState(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// 复制一份状态
    /// </summary>
    /// <returns>新的状态对象</returns>
    public DataFilterState Clone()
    {
        return new DataFilterState(IsEnabled);
    }
}
