namespace Rongia.Expressions;

/// <summary>
/// 表达式求值上下文: 提供 #root 与 #变量
/// </summary>
public sealed class ExpressionContext
{
    private readonly Dictionary<string, object?> _variables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 构造器
    /// </summary>
    /// <param name="root">#root 对象</param>
    public ExpressionContext(ExpressionRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);

        Root = root;
        _variables["root"] = root;
    }

    /// <summary>
    /// #root 对象
    /// </summary>
    public ExpressionRoot Root { get; }

    /// <summary>
    /// 全部变量(只读视图)
    /// </summary>
    public IReadOnlyDictionary<string, object?> Variables => _variables;

    /// <summary>
    /// 设置变量(如 #id / #result)
    /// </summary>
    /// <param name="name">变量名, 不含前缀 #</param>
    /// <param name="value">变量值</param>
    /// <returns>当前上下文, 便于链式调用</returns>
    public ExpressionContext SetVariable(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _variables[name] = value;

        return this;
    }

    /// <summary>
    /// 尝试读取变量
    /// </summary>
    /// <param name="name">变量名, 不含前缀 #</param>
    /// <param name="value">变量值</param>
    /// <returns>是否存在该变量</returns>
    public bool TryGetVariable(string name, out object? value) => _variables.TryGetValue(name, out value);
}
