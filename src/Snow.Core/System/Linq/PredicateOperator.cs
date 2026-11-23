using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace System.Linq;

// 以下代码取自 https://github.com/scottksmith95/LINQKit 项目。

/// <summary> 谓词组合方式 </summary>
public enum PredicateOperator
{
    /// <summary> 或 </summary>
    Or,

    /// <summary> 与 </summary>
    And
}

/// <summary>
/// 动态拼接表达式树的辅助类, 用法与示例见 http://www.albahari.com/expressions。
/// </summary>
public static class PredicateBuilder
{
    /// <summary>
    /// 把第二个表达式里的参数替换成第一个表达式的参数, 这样两个表达式才能拼成一个 lambda。
    /// </summary>
    private class RebindParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="oldParameter">被替换掉的参数</param>
        /// <param name="newParameter">替换成的参数</param>
        public RebindParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }

    /// <summary> 创建一个还没开始的表达式容器, 可以带上初始表达式。 </summary>
    public static ExpressionStarter<T> New<T>(Expression<Func<T, bool>>? expr = null)
    {
        return new ExpressionStarter<T>(expr);
    }

    /// <summary> 创建一个表达式容器, 并指定在表达式还没开始时用恒真还是恒假的占位表达式。 </summary>
    public static ExpressionStarter<T> New<T>(bool defaultExpression)
    {
        return new ExpressionStarter<T>(defaultExpression);
    }

    /// <summary> 用 OrElse 把两个表达式拼成"或"的关系。 </summary>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var expr2Body = new RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, expr2Body!), expr1.Parameters);
    }

    /// <summary> 用 AndAlso 把两个表达式拼成"与"的关系。 </summary>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var expr2Body = new RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, expr2Body!), expr1.Parameters);
    }

    /// <summary>
    /// 按指定的 <paramref name="operator"/> 把第二个谓词拼到第一个谓词后面。
    /// </summary>
    /// <typeparam name="T">谓词接受的参数类型</typeparam>
    /// <param name="first">源谓词</param>
    /// <param name="second">要拼上去的谓词</param>
    /// <param name="operator">组合方式, 取 And 或 Or, 默认 Or</param>
    /// <returns>拼好的新谓词</returns>
    public static Expression<Func<T, bool>> Extend<T>(this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second, PredicateOperator @operator = PredicateOperator.Or)
    {
        return @operator == PredicateOperator.Or ? first.Or(second) : first.And(second);
    }

    /// <summary>
    /// 按指定的 <paramref name="operator"/> 把第二个谓词拼到表达式容器后面。
    /// </summary>
    /// <typeparam name="T">谓词接受的参数类型</typeparam>
    /// <param name="first">源表达式容器</param>
    /// <param name="second">要拼上去的谓词</param>
    /// <param name="operator">组合方式, 取 And 或 Or, 默认 Or</param>
    /// <returns>拼好的新谓词</returns>
    public static Expression<Func<T, bool>> Extend<T>(this ExpressionStarter<T> first,
        Expression<Func<T, bool>> second, PredicateOperator @operator = PredicateOperator.Or)
    {
        return @operator == PredicateOperator.Or ? first.Or(second) : first.And(second);
    }
}

/// <summary>
/// 表达式容器: 表达式还没开始时用一个默认表达式顶着, 避免拼出 1=0 或 1=1 这种占位条件。
/// </summary>
/// <typeparam name="T">谓词接受的参数类型</typeparam>
public class ExpressionStarter<T>
{
    /// <summary> 默认用恒假表达式占位。 </summary>
    public ExpressionStarter() : this(false)
    {
    }

    /// <summary>
    /// 指定占位表达式: true 用恒真, false 用恒假。
    /// </summary>
    /// <param name="defaultExpression">占位时用恒真还是恒假</param>
    public ExpressionStarter(bool defaultExpression)
    {
        if (defaultExpression)
        {
            DefaultExpression = f => true;
        }
        else
        {
            DefaultExpression = f => false;
        }
    }

    /// <summary>
    /// 用一个已有表达式初始化容器。
    /// </summary>
    /// <param name="exp">初始表达式, 可以为 null</param>
    public ExpressionStarter(Expression<Func<T, bool>>? exp) : this(false)
    {
        _predicate = exp;
    }

    /// <summary>真正使用的谓词: 已经开始就用 _predicate, 否则退回默认表达式。</summary>
    private Expression<Func<T, bool>> Predicate =>
        ((IsStarted || !UseDefaultExpression) ? _predicate : DefaultExpression)!;

    private Expression<Func<T, bool>>? _predicate;

    /// <summary>表达式是否已经开始。</summary>
    public bool IsStarted => _predicate != null;

    /// <summary>是否配置了默认表达式(只有默认表达式不为 null 时才生效)。 </summary>
    public bool UseDefaultExpression => DefaultExpression != null;

    /// <summary>默认表达式 </summary>
    public Expression<Func<T, bool>>? DefaultExpression { get; set; }

    /// <summary>设置第一个表达式, 容器只能开始一次。</summary>
    /// <param name="exp">第一个表达式</param>
    public Expression<Func<T, bool>> Start(Expression<Func<T, bool>> exp)
    {
        if (IsStarted)
        {
            throw new Exception("Predicate cannot be started again.");
        }

        return _predicate = exp;
    }

    /// <summary>拼一个"或"上去</summary>
    public Expression<Func<T, bool>> Or(Expression<Func<T, bool>> expr2)
    {
        return (IsStarted) ? _predicate = Predicate.Or(expr2) : Start(expr2);
    }

    /// <summary>拼一个"与"上去</summary>
    public Expression<Func<T, bool>> And(Expression<Func<T, bool>> expr2)
    {
        return (IsStarted) ? _predicate = Predicate.And(expr2) : Start(expr2);
    }

    /// <summary> 输出谓词的字符串形式 </summary>
    public override string? ToString()
    {
        return Predicate?.ToString();
    }

    #region Implicit Operators

    /// <summary>
    /// 允许把容器隐式转成 <see cref="Expression{TDelegate}"/>。
    /// </summary>
    /// <param name="right">被转换的容器</param>
    public static implicit operator Expression<Func<T, bool>>?(ExpressionStarter<T>? right)
    {
        return right?.Predicate;
    }

    /// <summary>
    /// 允许把容器隐式转成已编译的委托。
    /// </summary>
    /// <param name="right">被转换的容器</param>
    public static implicit operator Func<T, bool>?(ExpressionStarter<T>? right)
    {
        return right == null ? null :
            (right.IsStarted || right.UseDefaultExpression) ? right.Predicate.Compile() : null;
    }

    /// <summary>
    /// 允许把表达式隐式包成容器。
    /// </summary>
    /// <param name="right">被包装的表达式</param>
    public static implicit operator ExpressionStarter<T>?(Expression<Func<T, bool>>? right)
    {
        return right == null ? null : new ExpressionStarter<T>(right);
    }

    #endregion

    #region Implement Expression<TDelagate> methods and properties

#if !(NET35)

    /// <summary>把当前谓词编译成可执行委托。</summary>
    public Func<T, bool> Compile()
    {
        return Predicate.Compile();
    }
#endif

#if !(NET35 || WINDOWS_APP || NETSTANDARD || PORTABLE || PORTABLE40 || UAP)
        /// <summary>按给定的调试信息生成器编译当前谓词。</summary>
        public Func<T, bool> Compile(DebugInfoGenerator debugInfoGenerator) { return Predicate.Compile(debugInfoGenerator); }

        /// <summary>用新的主体和参数生成一个新表达式。</summary>
        public Expression<Func<T, bool>> Update(Expression body, IEnumerable<ParameterExpression> parameters) { return Predicate.Update(body, parameters); }
#endif

    #endregion

    #region Implement LamdaExpression methods and properties

    /// <summary>当前谓词的表达式主体。</summary>
    public Expression Body => Predicate.Body;


    /// <summary>当前谓词的节点类型。</summary>
    public ExpressionType NodeType => Predicate.NodeType;

    /// <summary>当前谓词的参数列表。</summary>
    public ReadOnlyCollection<ParameterExpression> Parameters => Predicate.Parameters;

    /// <summary>当前谓词的静态类型。</summary>
    public Type Type => Predicate.Type;

#if !(NET35)
    /// <summary>当前谓词的名字。</summary>
    public string? Name => Predicate.Name;

    /// <summary>当前谓词的返回类型。</summary>
    public Type ReturnType => Predicate.ReturnType;

    /// <summary>当前谓词是否标记为尾调用。</summary>
    public bool TailCall => Predicate.TailCall;
#endif

    #endregion

    #region Implement Expression methods and properties

#if !(NET35)
    /// <summary>当前谓词能否被化简。</summary>
    public virtual bool CanReduce => Predicate.CanReduce;
#endif

    #endregion
}
