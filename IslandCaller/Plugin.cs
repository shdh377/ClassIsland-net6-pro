using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared;
using IslandCaller.Actions;
using IslandCaller.Helpers;
using IslandCaller.Models;
using IslandCaller.Services;
using IslandCaller.Services.IslandCallerService;
using IslandCaller.Shared;
using IslandCaller.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace IslandCaller;

[PluginEntrance]
public class Plugin : PluginBase
{
    public Window? HoverWindow { get; set; }

    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 数据统一存放到应用根目录 Data\IslandCaller，避免依赖 %AppData% 与注册表（生产机有还原系统）
        AppPaths.DataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "IslandCaller");
        services.AddSingleton<Status>();
        services.AddSingleton<IslandCallerNotificationProvider>();
        services.AddNotificationProvider<IslandCallerNotificationProvider>();
        services.AddSingleton<IslandCallerService>();
        services.AddSingleton<ProfileService>();
        services.AddSingleton<HistoryService>();
        services.AddSingleton<CoreService>();
        services.AddSingleton<WindowDragHelper>();
        services.AddSingleton<WindowTopmostHelper>();
        services.AddSettingsPage<SettingPage>();
        services.AddAction<DisableHoverAction>();
        services.AddAction<EnableHoverAction>();
        services.AddAction<CallAction>();

        AppBase.Current.AppStarted += async (_, _) =>
        {
            try
            {
                var logger = IAppHost.GetService<ILogger<Plugin>>();
                IAppHost.GetService<Status>();
                logger.LogInformation("插件状态初始化完成，正在加载设置...");
                new Settings(IAppHost.GetService<ProfileService>()).Load();
                logger.LogDebug("设置加载完成，正在加载默认配置...");
                IAppHost.GetService<ProfileService>().LoadSelectedProfile(Settings.Instance.Profile.DefaultProfile);
                logger.LogDebug("默认配置加载完成，正在加载历史记录...");
                IAppHost.GetService<HistoryService>().Load(Settings.Instance.Profile.DefaultProfile);
                logger.LogDebug("历史记录加载完成，正在初始化核心服务...");
                IAppHost.GetService<CoreService>().InitializeCore();
                logger.LogDebug("核心服务初始化完成，正在启动 IslandCaller 服务...");
                IAppHost.GetService<IslandCallerService>();
                logger.LogInformation("IslandCaller 插件初始化完成");
                if (Settings.Instance.Hover.IsEnable)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HoverWindow = new HoverFluent();
                        HoverWindow.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                var logger = IAppHost.GetService<ILogger<Plugin>>();
                logger.LogCritical(ex, "初始化失败");
                throw;
            }
        };
    }

    public void ShowHoverWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (HoverWindow == null)
            {
                HoverWindow = new HoverFluent();
                HoverWindow.Show();
            }
        });
    }

    public void HideHoverWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            HoverWindow?.Close();
            HoverWindow = null;
        });
    }
}
