using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using IslandCaller.Models;
using Microsoft.Extensions.Logging;

namespace IslandCaller.Actions;

[ActionInfo("IslandCaller.EnableHover", "启用悬浮窗")]
public class EnableHoverAction : ActionBase
{
    private readonly ILogger<EnableHoverAction> _logger = IAppHost.GetService<ILogger<EnableHoverAction>>();

    protected override async Task OnInvoke()
    {
        _logger.LogInformation("行动：启用悬浮窗");
        Settings.Instance.Hover.IsEnable = true;
        await Task.CompletedTask;
    }

    protected override async Task OnRevert()
    {
        _logger.LogInformation("行动：恢复启用悬浮窗");
        Settings.Instance.Hover.IsEnable = false;
        await Task.CompletedTask;
    }
}
