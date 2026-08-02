using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Models.Notification;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace IslandCaller.Services;

[NotificationProviderInfo(
    "9B570BF1-9A32-40C0-9D5D-4FFA69E03A37",
    "IslandCallerServices",
    "用于为IslandCaller提供通知接口")]
public class IslandCallerNotificationProvider : NotificationProviderBase
{
    public NotificationRequest? Request { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void RandomCall(int stunum, CoreService coreService)
    {
        var names = new List<string>();
        for (int i = 0; i < stunum; i++)
        {
            names.Add(coreService.GetRandomStudent());
        }

        string output = string.Join("  ", names);
        var duration = TimeSpan.FromSeconds(stunum * 2 + 1);

        var content = new TextBlock
        {
            Text = "🎯 " + output,
            FontSize = 28,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            TextAlignment = System.Windows.TextAlignment.Center
        };
        Request = new NotificationRequest()
        {
            MaskContent = content,
            MaskDuration = duration,
            MaskSpeechContent = output,
            IsSpeechEnabled = true
        };
        ShowNotification(Request);
    }
}
