using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Action;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 注册行动的<see cref="IServiceCollection"/>扩展。
/// </summary>
public static class ActionRegistryExtensions
{
    /// <summary>
    /// 注册一个行动提供方（类模式）。
    /// </summary>
    /// <typeparam name="TAction">行动提供方，继承自<see cref="ActionBase"/>。</typeparam>
    public static IServiceCollection AddAction<TAction>(this IServiceCollection services)
        where TAction : ActionBase
    {
        var actionType = typeof(TAction);
        var infoAttr = actionType.GetCustomAttributes(false).FirstOrDefault(x => x is ActionInfo) as ActionInfo;
        if (infoAttr == null)
            throw new InvalidOperationException($"无法注册行动提供方 {actionType.FullName}: 未标注 ActionInfo 特性。");

        var id = infoAttr.Id;

        // 注册为 keyed 服务
        services.AddKeyedTransient<ActionBase, TAction>(id);

        // 同时注册到旧的 delegate 系统以兼容
        if (!IActionService.Actions.ContainsKey(id))
        {
            var registryInfo = new ActionRegistryInfo(id, infoAttr.Name);
            IActionService.Actions.Add(id, registryInfo);
        }

        return services;
    }

    /// <summary>
    /// 注册一个行动提供方（类模式，带设置控件）。
    /// </summary>
    public static IServiceCollection AddAction<TAction, TSettingsControl>(this IServiceCollection services)
        where TAction : ActionBase
        where TSettingsControl : ActionSettingsControlBase
    {
        var actionType = typeof(TAction);
        var infoAttr = actionType.GetCustomAttributes(false).FirstOrDefault(x => x is ActionInfo) as ActionInfo;
        if (infoAttr == null)
            throw new InvalidOperationException($"无法注册行动提供方 {actionType.FullName}: 未标注 ActionInfo 特性。");

        var id = infoAttr.Id;

        services.AddKeyedTransient<ActionBase, TAction>(id);
        services.AddKeyedTransient<ActionSettingsControlBase, TSettingsControl>(id);

        if (!IActionService.Actions.ContainsKey(id))
        {
            var registryInfo = new ActionRegistryInfo(id, infoAttr.Name);
            registryInfo.SettingsControlType = typeof(TSettingsControl);
            IActionService.Actions.Add(id, registryInfo);
        }

        return services;
    }
    /// <summary>
    /// 注册无设置行动。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">行动ID，例如“classisland.example”。</param>
    /// <param name="name">行动名称。/</param>
    /// <param name="iconKind">行动图标。</param>
    /// <param name="onHandle">行动处理程序。</param>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddAction
        (this IServiceCollection services,
         string id,
         string name = "",
         PackIconKind iconKind = PackIconKind.BacteriaOutline,
         ActionRegistryInfo.HandleDelegate? onHandle = null)
    {
        Register(id, name, iconKind, onHandle);
        return services;
    }

    /// <summary>
    /// 注册行动。
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>对象。</param>
    /// <param name="id">行动ID，例如“classisland.example”。</param>
    /// <param name="name">行动名称。/</param>
    /// <param name="iconKind">行动图标。</param>
    /// <param name="onHandle">行动处理程序。</param>
    /// <typeparam name="TSettings">行动设置类型。</typeparam>
    /// <typeparam name="TSettingsControl">行动设置控件类型。</typeparam>
    /// <returns><see cref="IServiceCollection"/>对象。</returns>
    public static IServiceCollection AddAction<TSettings, TSettingsControl>
        (this IServiceCollection services,
         string id,
         string name = "",
         PackIconKind iconKind = PackIconKind.BacteriaOutline,
         ActionRegistryInfo.HandleDelegate? onHandle = null)
         where TSettingsControl : ActionSettingsControlBase
    {
        var info = Register(id, name, iconKind, onHandle);
        services.AddKeyedTransient<ActionSettingsControlBase, TSettingsControl>(id);
        info.SettingsType = typeof(TSettings);
        info.SettingsControlType = typeof(TSettingsControl);
        return services;
    }


    private static ActionRegistryInfo Register
        (string id,
         string name = "",
         PackIconKind iconKind = PackIconKind.BacteriaOutline,
         ActionRegistryInfo.HandleDelegate? onHandle = null)
    {
        if (IActionService.Actions.ContainsKey(id))
        {
            throw new InvalidOperationException($"已注册ID为 {id} 的行动。");
        }

        var info = new ActionRegistryInfo(id, name, iconKind);
        info.Handle += onHandle;
        IActionService.Actions.Add(id, info);

        return info;
    }
}