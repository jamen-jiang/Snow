using System;
using System.Collections.Generic;

namespace Snow.BackgroundJobs;

/// <summary>
/// 后台任务记录: 入队时写入, worker 取出后回写重试次数与下次执行时间
/// <para>只存"调哪个类型的方法、参数是什么", 不存实例 —— 执行时从容器现取。</para>
/// </summary>
public class BackgroundJobInfo
{
    /// <summary>
    /// 任务主键
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 承载方法的服务类型名, 见 <see cref="BackgroundJobTypeName"/>
    /// </summary>
    public string ServiceTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 方法名
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// 方法参数类型名, 与 <see cref="Arguments"/> 一一对应
    /// <para>存参数类型是为了执行时能精确重载匹配, 光靠方法名不够。</para>
    /// </summary>
    public List<string> ParameterTypeNames { get; set; } = [];

    /// <summary>
    /// 参数值(已序列化的 JSON), 与 <see cref="ParameterTypeNames"/> 一一对应
    /// <para>取消令牌参数在这里是空串, 执行时由 worker 注入。</para>
    /// </summary>
    public List<string> Arguments { get; set; } = [];

    /// <summary>
    /// 创建时间(UTC)
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 下次执行时间(UTC): 延迟任务与待重试任务靠它排序
    /// </summary>
    public DateTime NextTryTime { get; set; }

    /// <summary>
    /// 已尝试次数
    /// </summary>
    public int TryCount { get; set; }

    /// <summary>
    /// 便于日志与面板展示的名字, 形如 <c>Ns.MailService.SendAsync</c>
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(ServiceTypeName))
            {
                return MethodName;
            }

            int index = ServiceTypeName.IndexOf(',');

            return $"{(index > 0 ? ServiceTypeName[..index] : ServiceTypeName)}.{MethodName}";
        }
    }

    /// <summary>
    /// 浅拷贝: 存储对外只给副本, 免得调用方改到内部状态
    /// </summary>
    /// <returns>副本</returns>
    public BackgroundJobInfo Clone()
    {
        BackgroundJobInfo clone = (BackgroundJobInfo)MemberwiseClone();
        clone.ParameterTypeNames = [.. ParameterTypeNames];
        clone.Arguments = [.. Arguments];

        return clone;
    }
}
