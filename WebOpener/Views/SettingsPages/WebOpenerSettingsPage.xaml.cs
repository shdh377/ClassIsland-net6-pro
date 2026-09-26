using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.WebOpener.Models;
using ClassIsland.WebOpener.Services;
using ClassIsland.WebOpener.Views;

namespace ClassIsland.WebOpener.Views.SettingsPages;

[SettingsPageInfo("plugins.WebOpener", "定时打开网页", SettingsPageCategory.External)]
public partial class WebOpenerSettingsPage : SettingsPageBase
{
    private readonly WebSettingsStore _store;
    private readonly WebScheduleService _scheduler;

    public WebOpenerSettingsPage(WebSettingsStore store, WebScheduleService scheduler)
    {
        InitializeComponent();
        _store = store;
        _scheduler = scheduler;
        RefreshList();
        ApplyTriggerVisibility();
    }

    private void RefreshList()
    {
        RulesList.ItemsSource = null;
        RulesList.ItemsSource = _store.Rules;
    }

    private WebRule? SelectedRule => RulesList.SelectedItem as WebRule;

    private void RulesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedRule is not { } rule)
        {
            return;
        }
        TriggerCombo.SelectedIndex = rule.TriggerType == "weekly" ? 1 : 0;
        ApplyTriggerVisibility();
        SubjectBox.Text = rule.SubjectFilter;
        LeadBox.Text = rule.OpenLeadMinutes.ToString();
        TimeBox.Text = rule.Time;
        WeekdayCombo.SelectedIndex = rule.Weekday;
        UrlBox.Text = rule.Url;
        EnabledCheck.IsChecked = rule.Enabled;
    }

    private void TriggerCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyTriggerVisibility();
    }

    private void ApplyTriggerVisibility()
    {
        // XAML 初始化期间 TriggerCombo 自身的 SelectedIndex 会在字段赋值前触发事件，
        // 此时部分控件尚未创建，直接跳过；构造完成后会再显式调用一次。
        if (TriggerCombo is null || SubjectLabel is null || SubjectBox is null ||
            LeadLabel is null || LeadBox is null || TimeLabel is null || TimeBox is null ||
            WeekdayLabel is null || WeekdayCombo is null)
        {
            return;
        }

        var isClass = TriggerCombo.SelectedIndex == 0;
        var visibility = isClass ? Visibility.Visible : Visibility.Collapsed;
        SubjectLabel.Visibility = visibility;
        SubjectBox.Visibility = visibility;
        LeadLabel.Visibility = visibility;
        LeadBox.Visibility = visibility;

        var isWeekly = !isClass;
        var weeklyVisibility = isWeekly ? Visibility.Visible : Visibility.Collapsed;
        TimeLabel.Visibility = weeklyVisibility;
        TimeBox.Visibility = weeklyVisibility;
        WeekdayLabel.Visibility = weeklyVisibility;
        WeekdayCombo.Visibility = weeklyVisibility;
    }

    private bool TryReadCommon(out string error, out string url)
    {
        error = "";
        url = UrlBox.Text.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "请输入完整的网页地址（以 http:// 或 https:// 开头）。";
            return false;
        }

        if (TriggerCombo.SelectedIndex == 0)
        {
            if (string.IsNullOrWhiteSpace(SubjectBox.Text))
            {
                error = "科目名称不能为空。";
                return false;
            }
            if (!int.TryParse(LeadBox.Text.Trim(), out var lead) || lead < 1 || lead > 60)
            {
                error = "课前分钟数需要是 1–60 之间的整数。";
                return false;
            }
        }
        else
        {
            var time = TimeBox.Text.Trim();
            if (!TimeSpan.TryParse(time, out _) || !time.Contains(':'))
            {
                error = "时间格式应为 HH:mm，例如 14:00。";
                return false;
            }
        }
        return true;
    }

    private string TriggerTypeFromIndex(int index) => index == 1 ? "weekly" : "class";

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadCommon(out var error, out var url))
        {
            MessageBox.Show(error, "定时打开网页", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _store.Rules.Add(new WebRule
        {
            TriggerType = TriggerTypeFromIndex(TriggerCombo.SelectedIndex),
            SubjectFilter = SubjectBox.Text.Trim(),
            OpenLeadMinutes = TriggerCombo.SelectedIndex == 0
                ? Math.Clamp(int.Parse(LeadBox.Text.Trim()), 1, 60)
                : 1,
            Time = NormalizeTime(TimeBox.Text.Trim()),
            Weekday = Math.Clamp(WeekdayCombo.SelectedIndex, 0, 6),
            Url = url,
            Enabled = EnabledCheck.IsChecked == true
        });
        RefreshList();
    }

    private void ButtonUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedRule is not { } rule)
        {
            MessageBox.Show("请先在左侧选择要修改的计划。", "定时打开网页", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!TryReadCommon(out var error, out var url))
        {
            MessageBox.Show(error, "定时打开网页", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        rule.TriggerType = TriggerTypeFromIndex(TriggerCombo.SelectedIndex);
        rule.SubjectFilter = SubjectBox.Text.Trim();
        rule.OpenLeadMinutes = TriggerCombo.SelectedIndex == 0
            ? Math.Clamp(int.Parse(LeadBox.Text.Trim()), 1, 60)
            : 1;
        rule.Time = NormalizeTime(TimeBox.Text.Trim());
        rule.Weekday = Math.Clamp(WeekdayCombo.SelectedIndex, 0, 6);
        rule.Url = url;
        rule.Enabled = EnabledCheck.IsChecked == true;
        RefreshList();
        RulesList.SelectedItem = rule;
    }

    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedRule is not { } rule)
        {
            return;
        }
        _store.Rules.Remove(rule);
        RefreshList();
    }

    private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
    {
        _store.Save();
        MessageBox.Show("计划已保存。", "定时打开网页", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // 测试按钮：按表单当前内容弹出与到点时一致的确认对话框；
    // 点「打开」当场走真实打开链路，点「暂不打开」/超时仅关闭，不写入计划。
    private void ButtonTest_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadCommon(out var error, out var url))
        {
            MessageBox.Show(error, "定时打开网页", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var tempRule = new WebRule
        {
            TriggerType = TriggerTypeFromIndex(TriggerCombo.SelectedIndex),
            SubjectFilter = SubjectBox.Text.Trim(),
            OpenLeadMinutes = TriggerCombo.SelectedIndex == 0
                ? Math.Clamp(int.Parse(LeadBox.Text.Trim()), 1, 60)
                : 1,
            Time = NormalizeTime(TimeBox.Text.Trim()),
            Weekday = Math.Clamp(WeekdayCombo.SelectedIndex, 0, 6),
            Url = url
        };

        try
        {
            _scheduler.TestReminder(tempRule.TriggerLabel, url);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"测试触发失败：{ex.Message}", "定时打开网页", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string NormalizeTime(string time)
    {
        if (TimeSpan.TryParse(time, out var span)
            && span < TimeSpan.FromDays(1)
            && int.TryParse(time.Split(':')[0], out var hour))
        {
            return $"{hour:D2}:{span.Minutes:D2}";
        }
        return time;
    }
}
