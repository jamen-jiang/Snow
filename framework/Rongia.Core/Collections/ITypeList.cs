namespace Rongia.Collections;

/// <summary>
/// 类型清单: 元素必须是 <typeparamref name="TBase"/> 的子类或实现(对应 ABP 的 ITypeList)。
/// 与裸 List&lt;Type&gt; 的区别是写入时校验类型归属, 混入无关类型会立刻抛异常
/// </summary>
/// <typeparam name="TBase">元素允许的基类型</typeparam>
public interface ITypeList<in TBase> : IList<Type> where TBase : class
{
    /// <summary>
    /// 追加一个由类型参数指定的元素
    /// </summary>
    /// <typeparam name="T">元素类型,必须继承或实现 <typeparamref name="TBase"/></typeparam>
    void Add<T>() where T : TBase;

    /// <summary>
    /// 仅当清单里没有该类型时追加
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>实际追加时返回 true</returns>
    bool TryAdd<T>() where T : TBase;

    /// <summary>
    /// 判断清单里是否包含该类型
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>包含时返回 true</returns>
    bool Contains<T>() where T : TBase;

    /// <summary>
    /// 移除该类型
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    void Remove<T>() where T : TBase;
}
