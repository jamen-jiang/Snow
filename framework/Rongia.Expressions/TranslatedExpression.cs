namespace Rongia.Expressions;

/// <summary>
/// 翻译结果: C# 表达式文本 + 其中出现的 T(类型) / typeof(类型) 类型名列表(按 __speltype{i}__ 占位)
/// </summary>
/// <param name="Text">翻译后的 C# 表达式文本</param>
/// <param name="TypeNames">占位对应的类型名, 顺序与占位下标一致</param>
internal sealed record TranslatedExpression(string Text, IReadOnlyList<string> TypeNames);
