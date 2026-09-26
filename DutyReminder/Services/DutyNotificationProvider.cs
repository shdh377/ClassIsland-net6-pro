using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Models.Notification;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;

namespace ClassIsland.DutyReminder.Services;

/// <summary>
/// 值日提醒的通知提供方，负责向宿主投递提醒。
/// 横幅为「小图标 + 单行文字」；提醒内容为单行文字（不换行、超长省略）。
/// </summary>
[NotificationProviderInfo("6F1B7A3C-9D24-4E58-8A71-2C5D9E0B4F16", "值日提醒", "显示值日生擦黑板、值日提醒。")]
public class DutyNotificationProvider : NotificationProviderBase
{
    public void ShowDutyReminder(string text, bool speech)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // 宿主 ShowNotification 按【当前（UI）线程】调用栈识别提供方；从定时器线程
        // Dispatcher.Invoke 时提供方方法帧不在 UI 线程栈上会被判非法。故经由声明在
        // 本类型上的 Publish 方法调用，保证 UI 线程栈中存在提供方帧（定时触发才会走到这里）。
        Application.Current.Dispatcher.Invoke(() => Publish(BuildRequest(text, speech)));
    }

    private NotificationRequest BuildRequest(string text, bool speech)
    {
        return new NotificationRequest
        {
            MaskContent = BuildMaskLine(text),
            MaskDuration = TimeSpan.FromSeconds(4),
            OverlayContent = BuildSingleLine(text),
            OverlayDuration = TimeSpan.FromSeconds(10),
            MaskSpeechContent = speech ? text : "",
            IsSpeechEnabled = speech
        };
    }

    // 必须是实例方法且声明在本类型上：宿主按栈帧 DeclaringType 匹配已注册的提供方。
    private void Publish(NotificationRequest request) => ShowNotification(request);

    // 横幅：图标 + 文字同一行（横向 StackPanel 不会折行）。
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
            Kind = PackIconKind.Broom,
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

    // Mask 与 Overlay 不能共用同一个 UIElement，故各建一个实例。
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
