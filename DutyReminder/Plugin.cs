using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared;
using ClassIsland.DutyReminder.Models;
using ClassIsland.DutyReminder.Services;
using ClassIsland.DutyReminder.Views.SettingsPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.DutyReminder;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        var store = new DutySettingsStore(PluginConfigFolder);
        store.Load();
        services.AddSingleton(store);
        services.AddSingleton<DutyNotificationProvider>();
        services.AddNotificationProvider<DutyNotificationProvider>();
        services.AddSingleton<DutyScheduler>();
        services.AddSettingsPage<DutySettingsPage>();

        AppBase.Current.AppStarted += (_, _) =>
        {
            try
            {
                IAppHost.GetService<DutyScheduler>()?.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DutyReminder] 调度器启动失败：{ex}");
            }
        };
    }
}
