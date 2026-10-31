using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared;
using ClassIsland.WebKiosk.Models;
using ClassIsland.WebKiosk.Services;
using ClassIsland.WebKiosk.Views.SettingsPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClassIsland.WebKiosk;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        var store = new WebSettingsStore(PluginConfigFolder);
        store.Load();
        services.AddSingleton(store);
        services.AddSingleton<WebKioskNotificationProvider>();
        services.AddNotificationProvider<WebKioskNotificationProvider>();
        services.AddSingleton(sp => new KioskLauncher(PluginConfigFolder,
            sp.GetService<ILogger<KioskLauncher>>()));
        services.AddSingleton<WebScheduleService>();
        services.AddSettingsPage<WebKioskSettingsPage>();

        AppBase.Current.AppStarted += (_, _) =>
        {
            try
            {
                IAppHost.GetService<WebScheduleService>()?.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebKiosk] 调度器启动失败：{ex}");
            }
        };
    }
}
