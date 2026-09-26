using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClassIsland.Controls;
using ClassIsland.Core.Models.Weather;

namespace ClassIsland.Controls.NotificationProviders;

/// <summary>
/// WeatherHourlyForecastNotificationProvider.xaml 的交互逻辑
/// </summary>
public partial class WeatherHourlyForecastNotificationProvider : UserControl
{
    /// <summary>
    /// 逐小时预报展示的小时数。
    /// </summary>
    public const int SlotCount = 12;

    public bool IsOverlay { get; }
    public WeatherInfo Info { get; }
    public DateTime BaseTime { get; }

    public WeatherHourlyForecastNotificationProvider(bool isOverlay, WeatherInfo info, DateTime baseTime)
    {
        IsOverlay = isOverlay;
        Info = info;
        BaseTime = baseTime;
        InitializeComponent();
        BuildHourlySlots();
    }

    // 逐小时数据在构造时按可用长度生成，数据不足 12 条时只显示实际可用的条目，避免绑定越界。
    private void BuildHourlySlots()
    {
        var temperatures = Info.ForecastHourly.Temperature.Value;
        var weathers = Info.ForecastHourly.Weather.Value;
        var count = Math.Min(SlotCount, Math.Min(temperatures.Count, weathers.Count));
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                HourlyPanel.Children.Add(CreateSeparator());
            }

            var timeText = new TextBlock
            {
                Text = BaseTime.AddHours(i).ToString("HH:00"),
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            HourlyPanel.Children.Add(timeText);

            var icon = new WeatherPackIconControl
            {
                Code = weathers[i].ToString(),
                Height = 22,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 2, 0)
            };
            HourlyPanel.Children.Add(icon);

            var tempText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Medium
            };
            tempText.Inlines.Add(new Run(temperatures[i].ToString()));
            tempText.Inlines.Add(new Run("℃"));
            HourlyPanel.Children.Add(tempText);
        }
    }

    private static Separator CreateSeparator()
    {
        var separator = new Separator
        {
            Margin = new Thickness(8, 2, 8, 2)
        };
        var transforms = new TransformGroup();
        transforms.Children.Add(new ScaleTransform());
        transforms.Children.Add(new SkewTransform());
        transforms.Children.Add(new RotateTransform(90));
        separator.LayoutTransform = transforms;
        return separator;
    }
}
