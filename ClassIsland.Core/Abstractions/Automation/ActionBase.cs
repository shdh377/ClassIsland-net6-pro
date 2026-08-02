using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Abstractions.Automation;

/// <summary>
/// 行动提供方基类。
/// </summary>
public abstract class ActionBase
{
    /// <summary>
    /// 当此行动触发时，此方法将被调用。
    /// </summary>
    protected virtual async Task OnInvoke()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 当此行动恢复时，此方法将被调用。
    /// 重要：如果此行动提供方没有恢复行动，请勿重写此方法。
    /// </summary>
    protected virtual async Task OnRevert()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 当此行动运行被中断时，此方法将被调用。
    /// </summary>
    protected virtual async Task OnInterrupted()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// 行动提供方是否支持恢复。
    /// </summary>
    public bool IsRevertable { get; internal set; }

    internal object? SettingsInternal { get; set; }

    /// <summary>
    /// 获取行动提供方实例。
    /// </summary>
    public static ActionBase? GetInstance(string? actionId)
    {
        if (string.IsNullOrEmpty(actionId)) return null;
        return IAppHost.Host?.Services.GetKeyedService<ActionBase>(actionId);
    }
}

/// <summary>
/// 带设置类型的行动提供方基类。
/// </summary>
public abstract class ActionBase<TSettings> : ActionBase where TSettings : class
{
    /// <summary>
    /// 当前行动项的设置。
    /// </summary>
    protected TSettings Settings => (SettingsInternal as TSettings)!;
}
