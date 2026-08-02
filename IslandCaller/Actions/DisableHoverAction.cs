using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using IslandCaller.Models;
using Microsoft.Extensions.Logging;

namespace IslandCaller.Actions;

[ActionInfo("IslandCaller.DisableHover", "禁用悬浮窗")]
public class DisableHoverAction : ActionBase
{
    private readonly ILogger<DisableHoverAction> _logger = IAppHost.GetService<ILogger<DisableHoverAction>>();

    protected override async Task OnInvoke()
    {
        _logger.LogInformation("行动：禁用悬浮窗");
        Settings.Instance.Hover.IsEnable = false;
        await Task.CompletedTask;
    }

    protected override async Task OnRevert()
    {
        _logger.LogInformation("行动：恢复禁用悬浮窗");
        Settings.Instance.Hover.IsEnable = true;
        await Task.CompletedTask;
    }
}
