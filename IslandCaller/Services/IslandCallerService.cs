using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared.Enums;
using IslandCaller.Models;
using IslandCaller.Views;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace IslandCaller.Services.IslandCallerService;

public class IslandCallerService
{
    private ILessonsService LessonsService { get; }
    private ILogger<IslandCallerService> Logger { get; }
    private CoreService CoreService { get; }
    private Plugin Plugin { get; set; }
    private IslandCallerNotificationProvider NotificationProvider { get; }
    private IslandCallerNotificationProvider? LastRequest { get; set; }
    public Status Status { get; set; }

    public IslandCallerService(Plugin plugin,
                                IUriNavigationService uriNavigationService,
                                ILessonsService lessonsService,
                                HistoryService historyService,
                                CoreService coreService,
                                IslandCallerNotificationProvider notificationProvider,
                                Status status,
                                ILogger<IslandCallerService> logger)
    {
        LessonsService = lessonsService;
        CoreService = coreService;
        Logger = logger;
        Plugin = plugin;
        NotificationProvider = notificationProvider;
        Status = status;
        status.IslandCallerServiceInitialized = false;
        Status.IsTimeStatusAvailable = !(Settings.Instance.General.BreakDisable & lessonsService.CurrentState == TimeState.Breaking);
        Status.InterruptionEnable = Settings.Instance.General.Interruptable;
        lessonsService.CurrentTimeStateChanged += (s, e) =>
        {
            historyService.ClearThisLessonHistory();
            Status.IsTimeStatusAvailable = !(Settings.Instance.General.BreakDisable & lessonsService.CurrentState == TimeState.Breaking);
        };
        Settings.Instance.General.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Settings.Instance.General.BreakDisable))
            {
                Status.IsTimeStatusAvailable = !(Settings.Instance.General.BreakDisable & lessonsService.CurrentState == TimeState.Breaking);
            }
            if (e.PropertyName == nameof(Settings.Instance.General.Interruptable))
            {
                Status.InterruptionEnable = Settings.Instance.General.Interruptable;
            }
        };
        Settings.Instance.Hover.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Settings.Instance.Hover.IsEnable))
            {
                if (Settings.Instance.Hover.IsEnable)
                {
                    plugin.ShowHoverWindow();
                }
                else
                {
                    plugin.HideHoverWindow();
                }
            }
        };
        uriNavigationService.HandlePluginsNavigation(
            "IslandCaller/Simple",
            args => ShowRandomStudent(1)
        );
        uriNavigationService.HandlePluginsNavigation(
            "IslandCaller/Advanced/GUI",
            args =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = new PersonalCall();
                    window.Show();
                });
            }
        );
        status.IslandCallerServiceInitialized = true;
    }

    public async void ShowRandomStudent(int stunum)
    {
        if (Status.IsPluginReady == false) return;
        Status.OccupationDisable = false;
        try
        {
            if (Status.InterruptionEnable == true && LastRequest?.Request != null)
            {
                LastRequest.Request.Cancel();
                Logger.LogWarning("上一个点名请求已被取消");
            }
            NotificationProvider.RandomCall(stunum, CoreService);
            LastRequest = NotificationProvider;
            await Task.Delay(stunum * 2000 + 1000);
        }
        finally
        {
            Status.OccupationDisable = true;
            LastRequest = null;
        }
    }
}
