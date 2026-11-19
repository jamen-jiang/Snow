namespace Rongia.Messaging;

using System;
using System.Threading.Tasks;

/// <summary>
/// 空返回值类型:<see cref="System.Void"/> 不能作为泛型实参,故用一个只有单一取值的结构体占位
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    private static readonly Unit _value = new();

    /// <summary>
    /// <see cref="Unit"/> 的唯一取值
    /// </summary>
    public static ref readonly Unit Value => ref _value;

    /// <summary>
    /// 已完成的 <see cref="Unit"/> 任务
    /// </summary>
    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(_value);

    /// <inheritdoc />
    public int CompareTo(Unit other) => 0;

    /// <inheritdoc />
    int IComparable.CompareTo(object? obj) => 0;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// 判断两个 <see cref="Unit"/> 是否相等(恒为 true)
    /// </summary>
    /// <param name="first">左操作数</param>
    /// <param name="second">右操作数</param>
    /// <returns>恒为 <c>true</c></returns>
    public static bool operator ==(Unit first, Unit second) => true;

    /// <summary>
    /// 判断两个 <see cref="Unit"/> 是否不相等(恒为 false)
    /// </summary>
    /// <param name="first">左操作数</param>
    /// <param name="second">右操作数</param>
    /// <returns>恒为 <c>false</c></returns>
    public static bool operator !=(Unit first, Unit second) => false;

    /// <inheritdoc />
    public override string ToString() => "()";
}
