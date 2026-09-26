using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.DutyReminder.Models;
using ClassIsland.DutyReminder.Services;
using ClassIsland.Shared;

namespace ClassIsland.DutyReminder.Views.SettingsPages;

[SettingsPageInfo("plugins.DutyReminder", "值日提醒", SettingsPageCategory.External)]
public partial class DutySettingsPage : SettingsPageBase
{
    private readonly DutySettingsStore _store;

    public DutySettingsPage(DutySettingsStore store)
    {
        InitializeComponent();
        _store = store;
        RefreshList();
        ApplyTriggerVisibility();
    }

    private void RefreshList()
    {
        RulesList.ItemsSource = null;
        RulesList.ItemsSource = _store.Rules;
    }

    private DutyRule? SelectedRule => RulesList.SelectedItem as DutyRule;

    private void RulesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedRule is not { } rule)
        {
            return;
        }
        // 历史数据中的 weekly 也按「自定义时间」展示
        TriggerCombo.SelectedIndex = rule.TriggerType == "class" ? 0 : 1;
        ApplyTriggerVisibility();
        LeadBox.Text = rule.LeadMinutes.ToString();
        TimeBox.Text = rule.Time;
        TextBlockBox.Text = rule.Text;
        SpeechCheck.IsChecked = rule.Speech;
        EnabledCheck.IsChecked = rule.Enabled;
    }

    private void TriggerCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyTriggerVisibility();
    }

    // 触发方式二选一：只显示当前触发方式对应的字段。
    // XAML 初始化期间事件可能先于字段赋值触发，此时控件尚未创建，直接跳过；
    // 构造完成后会显式再调用一次以应用初始可见性。
    private void ApplyTriggerVisibility()
    {
        if (TriggerCombo is null || LeadPanel is null || TimePanel is null)
        {
            return;
        }
        var isClass = TriggerCombo.SelectedIndex == 0;
        LeadPanel.Visibility = isClass ? Visibility.Visible : Visibility.Collapsed;
        TimePanel.Visibility = isClass ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PresetCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetCombo.SelectedItem is ComboBoxItem item && item.Content is string preset)
        {
            TextBlockBox.Text = preset;
        }
    }

    private bool TryReadCommon(out string error, out int lead, out string time)
    {
        error = "";
        lead = 2;
        time = TimeBox.Text.Trim();

        var isClass = TriggerCombo.SelectedIndex == 0;
        if (isClass)
        {
            if (!int.TryParse(LeadBox.Text.Trim(), out lead) || lead < 1 || lead > 60)
            {
                error = "提前分钟数需要是 1–60 之间的整数。";
                return false;
            }
        }
        else if (!TimeSpan.TryParse(time, out _) || !time.Contains(':'))
        {
            error = "时间格式应为 HH:mm，例如 07:40。";
            return false;
        }
        if (string.IsNullOrWhiteSpace(TextBlockBox.Text))
        {
            error = "提醒内容不能为空。";
            return false;
        }
        return true;
    }

    private string TriggerTypeFromIndex(int index) => index == 0 ? "class" : "daily";

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryReadCommon(out var error, out var lead, out var time))
        {
            MessageBox.Show(error, "值日提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _store.Rules.Add(new DutyRule
        {
            TriggerType = TriggerTypeFromIndex(TriggerCombo.SelectedIndex),
            LeadMinutes = lead,
            Time = NormalizeTime(time),
            Text = TextBlockBox.Text.Trim(),
            Speech = SpeechCheck.IsChecked == true,
            Enabled = EnabledCheck.IsChecked == true
        });
        RefreshList();
    }

    private void ButtonUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedRule is not { } rule)
        {
            MessageBox.Show("请先在左侧选择要修改的规则。", "值日提醒", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!TryReadCommon(out var error, out var lead, out var time))
        {
            MessageBox.Show(error, "值日提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        rule.TriggerType = TriggerTypeFromIndex(TriggerCombo.SelectedIndex);
        rule.LeadMinutes = lead;
        rule.Time = NormalizeTime(time);
        rule.Text = TextBlockBox.Text.Trim();
        rule.Speech = SpeechCheck.IsChecked == true;
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
        MessageBox.Show("规则已保存。", "值日提醒", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // 测试按钮：立即按表单中的内容与语音开关触发一次真实提醒，不写入规则。
    private void ButtonTest_OnClick(object sender, RoutedEventArgs e)
    {
        var text = TextBlockBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("提醒内容不能为空。", "值日提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var provider = IAppHost.GetService<DutyNotificationProvider>();
        if (provider == null)
        {
            MessageBox.Show("提醒服务尚未就绪，请重启应用后重试。", "值日提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        provider.ShowDutyReminder(text, SpeechCheck.IsChecked == true);
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
