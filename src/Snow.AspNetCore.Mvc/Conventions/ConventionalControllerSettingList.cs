using System;
using System.Collections.Generic;
using System.Linq;

namespace Snow.AspNetCore.Mvc.Conventions;

/// <summary>
/// 约定控制器设置列表(对应 ABP 同名类型)。
/// </summary>
public class ConventionalControllerSettingList : List<ConventionalControllerSetting>
{
    /// <summary>
    /// 按控制器类型找到对应的设置, 找不到返回 null。
    /// </summary>
    public ConventionalControllerSetting? GetSettingOrNull(Type controllerType)
    {
        return this.FirstOrDefault(controllerSetting => controllerSetting.ControllerTypes.Contains(controllerType));
    }
}