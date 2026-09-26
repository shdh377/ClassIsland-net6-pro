using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared;
using ClassIsland.WebOpener.Models;
using ClassIsland.WebOpener.Services;
using ClassIsland.WebOpener.Views.SettingsPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.WebOpener;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        var store = new WebSettingsStore(PluginConfigFolder);
        store.Load();
        services.AddSingleton(store);
        services.AddSingleton<WebOpenerNotificationProvider>();
        services.AddNotificationProvider<WebOpenerNotificationProvider>();
        services.AddSingleton<WebScheduleService>();
        services.AddSettingsPage<WebOpenerSettingsPage>();

        AppBase.Current.AppStarted += (_, _) =>
        {
            try
            {
                IAppHost.GetService<WebScheduleService>()?.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebOpener] 调度器启动失败：{ex}");
            }
        };
    }
}
