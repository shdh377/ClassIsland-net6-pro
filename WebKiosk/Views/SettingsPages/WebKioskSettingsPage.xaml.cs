using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.WebKiosk.Models;
using ClassIsland.WebKiosk.Services;
using Microsoft.Win32;

namespace ClassIsland.WebKiosk.Views.SettingsPages;

[SettingsPageInfo("plugins.WebKiosk", "定时全屏播放", SettingsPageCategory.External)]
public partial class WebKioskSettingsPage : SettingsPageBase
{
    private readonly WebSettingsStore _store;
    private readonly WebScheduleService _scheduler;

    public WebKioskSettingsPage(WebSettingsStore store, WebScheduleService scheduler)
    {
        InitializeComponent();
        _store = store;
        _scheduler = scheduler;
        RefreshList();
        RefreshSilenceList();
        ApplyTriggerVisibility();
        ApplyTargetVisibility();
    }

    private void RefreshList()
    {
        RulesList.ItemsSource = null;
        RulesList.ItemsSource = _store.Rules;
    }

    private void RefreshSilenceList()
    {
        SilenceList.ItemsSource = null;
        SilenceList.ItemsSource = _store.SilenceWindows;
    }

    private WebRule? SelectedRule => RulesList.SelectedItem as WebRule;

    private void RulesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedRule is not { } rule)
        {
            return;
        }
        TriggerCombo.SelectedIndex = rule.TriggerType == "weekly" ? 1 : 0;
        TargetCombo.SelectedIndex = rule.IsFile ? 1 : 0;
        ApplyTriggerVisibility();
        ApplyTargetVisibility();
        SubjectBox.Text = rule.SubjectFilter;
        LeadBox.Text = rule.OpenLeadMinutes.ToString();
        TimeBox.Text = rule.Time;
        WeekdayCombo.SelectedIndex = rule.Weekday;
        UrlBox.Text = rule.Url;
        KioskCheck.IsChecked = rule.KioskMode;
        FilePathBox.Text = rule.FilePath;
        FullscreenCheck.IsChecked = rule.FullscreenImage;
        SecondsBox.Text = rule.ImageDisplaySeconds.ToString();
        EnabledCheck.IsChecked = rule.Enabled;
    }

    private void TriggerCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyTriggerVisibility();
    }

    private void TargetCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyTargetVisibility();
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

    private void ApplyTargetVisibility()
    {
        if (TargetCombo is null || UrlPanel is null || FilePanel is null)
        {
            return;
        }
        var isFile = TargetCombo.SelectedIndex == 1;
        UrlPanel.Visibility = isFile ? Visibility.Collapsed : Visibility.Visible;
        FilePanel.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>校验表单；成功时返回可直接落库的字段值。</summary>
    private bool TryReadCommon(out string error, out WebRule draft)
    {
        error = "";
        draft = new WebRule
        {
            TriggerType = TriggerCombo.SelectedIndex == 0 ? "class" : "weekly",
            TargetType = TargetCombo.SelectedIndex == 1 ? "file" : "url",
            SubjectFilter = SubjectBox.Text.Trim(),
            OpenLeadMinutes = 1,
            Time = NormalizeTime(TimeBox.Text.Trim()),
            Weekday = Math.Clamp(WeekdayCombo.SelectedIndex, 0, 6),
            Url = UrlBox.Text.Trim(),
            KioskMode = TargetCombo.SelectedIndex != 1 && KioskCheck.IsChecked == true,
            FilePath = FilePathBox.Text.Trim(),
            FullscreenImage = FullscreenCheck.IsChecked == true,
            ImageDisplaySeconds = 0,
            Enabled = EnabledCheck.IsChecked == true
        };

        if (TriggerCombo.SelectedIndex == 0)
        {
            if (string.IsNullOrWhiteSpace(draft.SubjectFilter))
            {
                error = "科目名称不能为空。";
                return false;
            }
            if (!int.TryParse(LeadBox.Text.Trim(), out var lead) || lead < 1 || lead > 60)
            {
                error = "课前分钟数需要是 1–60 之间的整数。";
                return false;
            }
            draft.OpenLeadMinutes = lead;
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

        if (draft.IsFile)
        {
            if (string.IsNullOrWhiteSpace(draft.FilePath))
            {
                error = "请先选择要打开的文件。";
                return false;
            }
            if (!int.TryParse(SecondsBox.Text.Trim(), out var seconds) || seconds < 0 || seconds > 3600)
            {
                error = "图片全屏时长需要是 0–3600 之间的整数（0 表示不自动关闭）。";
                return false;
            }
            draft.ImageDisplaySeconds = seconds;

            if (!File.Exists(draft.FilePath) &&
                MessageBox.Show($"文件当前不存在：\n{draft.FilePath}\n\n仍要保存该计划吗？", "定时全屏播放",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
            {
                error = "";
                return false;
            }
        }
        else if (!Uri.TryCreate(draft.Url, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "请输入完整的网页地址（以 http:// 或 https:// 开头）。";
            return false;
        }

        return true;
    }

    private void ApplyDraft(WebRule target, WebRule draft)
    {
        target.TriggerType = draft.TriggerType;
        target.TargetType = draft.TargetType;
        target.SubjectFilter = draft.SubjectFilter;
        target.OpenLeadMinutes = draft.OpenLeadMinutes;
        target.Time = draft.Time;
        target.Weekday = draft.Weekday;
        target.Url = draft.Url;
        target.FilePath = draft.FilePath;
        target.FullscreenImage = draft.FullscreenImage;
        target.ImageDisplaySeconds = draft.ImageDisplaySeconds;
        target.KioskMode = draft.KioskMode;
        target.Enabled = draft.Enabled;
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadCommon(out var error, out var draft))
        {
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }
        _store.Rules.Add(draft);
        RefreshList();
    }

    private void ButtonUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedRule is not { } rule)
        {
            MessageBox.Show("请先在左侧选择要修改的计划。", "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!TryReadCommon(out var error, out var draft))
        {
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }
        ApplyDraft(rule, draft);
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

    private void ButtonBrowse_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要自动打开的文件",
            Filter = WebFileHelper.OpenFileFilter,
            CheckFileExists = true
        };
        if (dialog.ShowDialog() == true)
        {
            FilePathBox.Text = dialog.FileName;
            if (WebFileHelper.IsImageFile(dialog.FileName))
            {
                FullscreenCheck.IsChecked = true;
            }
        }
    }

    private void ButtonSave_OnClick(object sender, RoutedEventArgs e)
    {
        _store.Save();
        MessageBox.Show("计划已保存。", "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ButtonAddSilence_OnClick(object sender, RoutedEventArgs e)
    {
        if (!SilenceWindow.TryParse(SilenceStartBox.Text, out var start) ||
            !SilenceWindow.TryParse(SilenceEndBox.Text, out var end))
        {
            MessageBox.Show("静默时段格式应为 HH:mm，例如 12:00。", "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (start == end)
        {
            MessageBox.Show("静默时段的开始与结束时间不能相同。", "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _store.SilenceWindows.Add(new SilenceWindow
        {
            Start = $"{start.Hours:D2}:{start.Minutes:D2}",
            End = $"{end.Hours:D2}:{end.Minutes:D2}"
        });
        RefreshSilenceList();
    }

    private void ButtonDeleteSilence_OnClick(object sender, RoutedEventArgs e)
    {
        if (SilenceList.SelectedItem is not SilenceWindow window)
        {
            return;
        }
        _store.SilenceWindows.Remove(window);
        RefreshSilenceList();
    }

    // 测试按钮：按表单当前内容弹出与到点时一致的确认对话框；
    // 点「打开」当场走真实打开链路，点「暂不打开」/超时仅关闭，不写入计划。
    private void ButtonTest_OnClick(object sender, RoutedEventArgs e)    {
        if (!TryReadCommon(out var error, out var draft))
        {
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        try
        {
            _scheduler.TestReminder(draft);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"测试触发失败：{ex.Message}", "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 关闭本插件打开的 kiosk 全屏窗口（按 PID 精确关闭，不碰用户自己的浏览器窗口）。
    private void ButtonCloseKiosk_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var closed = _scheduler.CloseKiosk();
            MessageBox.Show(closed ? "已关闭全屏窗口。" : "没有检测到本插件打开的全屏窗口。",
                "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"关闭失败：{ex.Message}", "定时全屏播放", MessageBoxButton.OK, MessageBoxImage.Error);
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
