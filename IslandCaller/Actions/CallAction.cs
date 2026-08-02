using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using IslandCaller.Services.IslandCallerService;
using Microsoft.Extensions.Logging;

namespace IslandCaller.Actions;

[ActionInfo("IslandCaller.Call", "随机点名")]
public class CallAction : ActionBase
{
    private readonly ILogger<CallAction> _logger = IAppHost.GetService<ILogger<CallAction>>();
    private readonly IslandCallerService _islandCallerService = IAppHost.GetService<IslandCallerService>();

    protected override async Task OnInvoke()
    {
        _logger.LogInformation("行动：随机点名");
        _islandCallerService.ShowRandomStudent(1);
        await Task.CompletedTask;
    }
}
