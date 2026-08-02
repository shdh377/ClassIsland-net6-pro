using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 用于注册提醒提供方的 IServiceCollection 扩展。
/// </summary>
public static class NotificationProviderRegistryExtensions
{
    /// <summary>
    /// 注册一个提醒提供方
    /// </summary>
    public static IServiceCollection AddNotificationProvider<TNotificationProvider>(this IServiceCollection services)
        where TNotificationProvider : NotificationProviderBase
    {
        Register(typeof(TNotificationProvider));
        services.AddHostedService<TNotificationProvider>();
        return services;
    }

    /// <summary>
    /// 注册一个提醒提供方（带设置控件）
    /// </summary>
    public static IServiceCollection AddNotificationProvider<TNotificationProvider, TSettingsControl>(this IServiceCollection services)
        where TNotificationProvider : NotificationProviderBase
        where TSettingsControl : NotificationProviderControlBase
    {
        var info = Register(typeof(TNotificationProvider), typeof(TSettingsControl));
        services.AddHostedService<TNotificationProvider>();
        return services;
    }

    private static NotificationProviderInfoAttribute Register(Type notificationProvider, Type? settings = null)
    {
        var info = notificationProvider.GetCustomAttributes(false)
            .FirstOrDefault(x => x is NotificationProviderInfoAttribute) as NotificationProviderInfoAttribute;

        if (info == null)
        {
            throw new ArgumentException($"无法注册提醒提供方，因为 {notificationProvider.FullName} 没有 NotificationProviderInfo 特性。");
        }

        info.ProviderType = notificationProvider;
        if (notificationProvider.BaseType?.GenericTypeArguments.Length > 0)
        {
            info.HasSettings = true;
        }

        if (settings != null)
        {
            info.SettingsType = settings;
        }

        return info;
    }
}

/// <summary>
/// 提醒提供方设置控件基类（占位，用于泛型约束）
/// </summary>
public abstract class NotificationProviderControlBase : ClassIsland.Core.Abstractions.Controls.SettingsPageBase
{
}
