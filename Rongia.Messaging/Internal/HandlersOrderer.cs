namespace Rongia.Messaging.Internal;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 处理器排序器:把"离请求类型最近"的处理器排在前面,并剔除被派生类处理器覆盖的基类处理器
/// </summary>
internal static class HandlersOrderer
{
    /// <summary>
    /// 对处理器列表去重并按优先级排序
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    /// <param name="handlers">原始处理器列表</param>
    /// <param name="request">当前请求实例,用于计算相对距离</param>
    /// <returns>排序后的处理器列表</returns>
    public static IList<object> Prioritize<TRequest>(IList<object> handlers, TRequest request)
        where TRequest : notnull
    {
        if (handlers.Count < 2)
        {
            return handlers;
        }

        var requestObjectDetails = new ObjectDetails(request);
        var handlerObjectsDetails = handlers.Select(static s => new ObjectDetails(s)).ToList();

        var uniqueHandlers = RemoveOverridden(handlerObjectsDetails).ToArray();
        Array.Sort(uniqueHandlers, requestObjectDetails);

        return uniqueHandlers.Select(static s => s.Value).ToList();
    }

    /// <summary>
    /// 剔除被派生类型处理器覆盖的基类处理器(同一处理器族只保留最具体的一个)
    /// </summary>
    /// <param name="handlersData">处理器元数据列表</param>
    /// <returns>去重后的处理器元数据</returns>
    private static IEnumerable<ObjectDetails> RemoveOverridden(IList<ObjectDetails> handlersData)
    {
        for (var i = 0; i < handlersData.Count - 1; i++)
        {
            for (var j = i + 1; j < handlersData.Count; j++)
            {
                if (handlersData[i].IsOverridden || handlersData[j].IsOverridden)
                {
                    continue;
                }

                if (handlersData[i].Type.IsAssignableFrom(handlersData[j].Type))
                {
                    handlersData[i].IsOverridden = true;
                }
                else if (handlersData[j].Type.IsAssignableFrom(handlersData[i].Type))
                {
                    handlersData[j].IsOverridden = true;
                }
            }
        }

        return handlersData.Where(static w => !w.IsOverridden);
    }
}
