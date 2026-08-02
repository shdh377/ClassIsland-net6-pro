using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using ClassIsland.Shared.Models.Notification;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Abstractions.Services.NotificationProviders;

/// <summary>
/// 提醒提供方基类。
/// </summary>
public abstract class NotificationProviderBase : INotificationProvider, IHostedService
{
    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public Guid ProviderGuid { get; set; }

    /// <inheritdoc />
    public object? SettingsElement { get; set; }

    /// <inheritdoc />
    public object? IconElement { get; set; }

    internal object SettingsInternal { get; set; } = null!;

    internal INotificationHostService NotificationHostService { get; }

    private NotificationProviderInfoAttribute Info { get; }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// 初始化一个 NotificationProviderBase 类的新实例。
    /// </summary>
    protected NotificationProviderBase() : this(true)
    {
    }

    /// <summary>
    /// 初始化一个 NotificationProviderBase 类的新实例。
    /// </summary>
    protected NotificationProviderBase(bool autoRegister)
    {
        NotificationHostService = IAppHost.GetService<INotificationHostService>();

        var info = GetType().GetCustomAttributes(typeof(NotificationProviderInfoAttribute), false)
            .FirstOrDefault() as NotificationProviderInfoAttribute;

        Info = info ?? throw new InvalidOperationException($"没有找到与 {GetType()} 对应的提醒提供方信息。");

        Name = Info.Name;
        Description = Info.Description;
        ProviderGuid = Info.Guid;

        // 设置图标
        IconElement = new PackIcon()
        {
            Kind = PackIconKind.Notifications,
            Width = 24,
            Height = 24
        };

        if (!autoRegister)
        {
            return;
        }
        NotificationHostService.RegisterNotificationProvider(this);
    }

    /// <summary>
    /// 显示提醒。
    /// </summary>
    public void ShowNotification(NotificationRequest request)
    {
        NotificationHostService.ShowNotification(request);
    }

    /// <summary>
    /// 显示提醒，并等待提醒显示完成。
    /// </summary>
    public async Task ShowNotificationAsync(NotificationRequest request)
    {
        await NotificationHostService.ShowNotificationAsync(request);
    }
}

/// <summary>
/// 带设置类型的提醒提供方基类。
/// </summary>
public abstract class NotificationProviderBase<TSettings> : NotificationProviderBase where TSettings : class
{
    /// <summary>
    /// 当前提醒提供方的设置。
    /// </summary>
    public TSettings Settings => (SettingsInternal as TSettings)!;

    /// <inheritdoc />
    protected NotificationProviderBase() : this(true)
    {
    }

    /// <inheritdoc />
    protected NotificationProviderBase(bool autoRegister) : base(autoRegister)
    {
        if (!autoRegister)
        {
            return;
        }
        SettingsInternal = NotificationHostService.GetNotificationProviderSettings<TSettings>(ProviderGuid);
    }
}
