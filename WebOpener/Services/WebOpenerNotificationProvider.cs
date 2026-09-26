using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Models.Notification;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace ClassIsland.WebOpener.Services;

/// <summary>
/// 定时打开网页的横幅提醒提供方：复用 ClassIsland 通知系统展示单行横幅/提醒。
/// </summary>
[NotificationProviderInfo("7E2B4D5A-1C8F-4B3E-9A60-5D2F8C7E1A44", "打开网页提醒", "显示定时打开网页前的横幅提醒。")]
public class WebOpenerNotificationProvider : NotificationProviderBase
{
    public void ShowOpenWebReminder(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // 宿主 ShowNotification 通过遍历【当前（UI）线程】调用栈识别提供方类型；
        // 从定时器线程 Dispatcher.Invoke 时提供方方法帧不在 UI 线程栈上会被判定为非法调用。
        // 故在 lambda 内先走一个声明在本类型上的 Publish 方法，保证 UI 线程栈里有提供方帧。
        Application.Current.Dispatcher.Invoke(() => Publish(BuildRequest(text)));
    }

    private NotificationRequest BuildRequest(string text)
    {
        return new NotificationRequest
        {
            MaskContent = BuildMaskLine(text),
            MaskDuration = TimeSpan.FromSeconds(4),
            OverlayContent = BuildSingleLine(text),
            OverlayDuration = TimeSpan.FromSeconds(8),
            MaskSpeechContent = "",
            IsSpeechEnabled = false
        };
    }

    // 必须是实例方法且声明在本类型上：宿主按栈帧 DeclaringType 匹配已注册的提供方。
    private void Publish(NotificationRequest request) => ShowNotification(request);

    private static StackPanel BuildMaskLine(string text)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(new PackIcon
        {
            Kind = PackIconKind.Web,
            Width = 22,
            Height = 22,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 0, 0),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        return panel;
    }

    private static TextBlock BuildSingleLine(string text) => new()
    {
        Text = text,
        FontSize = 24,
        FontWeight = FontWeights.Bold,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.NoWrap,
        TextTrimming = TextTrimming.CharacterEllipsis
    };
}
