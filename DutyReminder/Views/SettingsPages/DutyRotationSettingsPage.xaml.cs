using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.DutyReminder.Models;
using ClassIsland.DutyReminder.Services;
using ClassIsland.Shared;
using Microsoft.Win32;

namespace ClassIsland.DutyReminder.Views.SettingsPages;

[SettingsPageInfo("plugins.DutyReminder.rotation", "值日小组轮换", SettingsPageCategory.External)]
public partial class DutyRotationSettingsPage : SettingsPageBase
{
    private readonly DutySettingsStore _store;

    public DutyRotationSettingsPage(DutySettingsStore store)
    {
        InitializeComponent();
        _store = store;

        RosterTextBox.Text = FormatGroups(_store.Groups);
        ApplyRotationToForm(_store.Rotation);
        RefreshGroups();
        RefreshRecords();
        ApplyWeekdayVisibility();
        UpdateCurrentGroupText();

        // 轮换基准变化时实时刷新「当前应值小组」预览。
        AnchorDateBox.LostFocus += (_, _) => UpdateCurrentGroupText();
        AnchorIndexBox.LostFocus += (_, _) => UpdateCurrentGroupText();
    }

    private static string FormatGroups(IEnumerable<DutyGroup> groups) =>
        string.Join(Environment.NewLine, groups.Select(g => $"{g.Name}：{g.MembersText}"));

    private void RefreshGroups()
    {
        GroupsList.ItemsSource = null;
        GroupsList.ItemsSource = DutyRosterParser.Parse(RosterTextBox.Text);
    }

    private void RefreshRecords()
    {
        RecordsList.ItemsSource = null;
        RecordsList.ItemsSource = _store.Records;
    }

    private void IntervalCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyWeekdayVisibility();
        UpdateCurrentGroupText();
    }

    private void ApplyWeekdayVisibility()
    {
        if (WeekdayPanel is null || IntervalCombo is null)
        {
            return;
        }
        // 仅「每周轮换」需要选择星期。
        WeekdayPanel.Visibility = IntervalCombo.SelectedIndex == 1 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyRotationToForm(DutyRotationSettings rotation)
    {
        RotationEnabledCheck.IsChecked = rotation.Enabled;
        IntervalCombo.SelectedIndex = rotation.IsDaily ? 1 : 0;
        WeekdayCombo.SelectedIndex = Math.Clamp(rotation.Weekday, 0, 6);
        RotTimeBox.Text = rotation.Time;
        AnchorDateBox.Text = rotation.ResolveAnchorDate().ToString("yyyy-MM-dd");
        AnchorIndexBox.Text = rotation.AnchorGroupIndex.ToString();
        TemplateBox.Text = rotation.TextTemplate;
        RotSpeechCheck.IsChecked = rotation.Speech;
    }

    /// <summary>从表单读取轮换设置；校验失败时返回 false 并给出提示。</summary>
    private bool TryBuildRotation(out DutyRotationSettings rotation, out string error)
    {
        error = "";
        rotation = new DutyRotationSettings
        {
            Enabled = RotationEnabledCheck.IsChecked == true,
            Interval = IntervalCombo.SelectedIndex == 1 ? "daily" : "weekly",
            Weekday = Math.Clamp(WeekdayCombo.SelectedIndex, 0, 6),
            Time = NormalizeTime(RotTimeBox.Text.Trim()),
            AnchorDate = AnchorDateBox.Text.Trim(),
            AnchorGroupIndex = 1,
            TextTemplate = TemplateBox.Text.Trim(),
            Speech = RotSpeechCheck.IsChecked == true
        };

        if (!TimeSpan.TryParse(rotation.Time, out _) || !rotation.Time.Contains(':'))
        {
            error = "提醒时间格式应为 HH:mm，例如 07:40。";
            return false;
        }
        if (!DateTime.TryParse(rotation.AnchorDate, out var anchor))
        {
            error = "轮换基准日期格式应为 yyyy-MM-dd，例如 2026-09-01。";
            return false;
        }
        rotation.AnchorDate = anchor.ToString("yyyy-MM-dd");
        if (!int.TryParse(AnchorIndexBox.Text.Trim(), out var anchorIndex) || anchorIndex < 1)
        {
            error = "基准组序号需要是不小于 1 的整数。";
            return false;
        }
        rotation.AnchorGroupIndex = anchorIndex;
        if (string.IsNullOrWhiteSpace(rotation.TextTemplate))
        {
            rotation.TextTemplate = "值日小组：{group}（{members}）";
        }
        return true;
    }

    private void UpdateCurrentGroupText()
    {
        if (CurrentGroupText is null || AnchorDateBox is null || AnchorIndexBox is null || IntervalCombo is null)
        {
            return;
        }

        var groups = DutyRosterParser.Parse(RosterTextBox.Text);
        if (groups.Count == 0)
        {
            CurrentGroupText.Text = "当前应值小组：—（请先填写或导入值日生表）";
            return;
        }
        if (!TryBuildRotation(out var rotation, out _))
        {
            CurrentGroupText.Text = "当前应值小组：—（轮换设置填写有误）";
            return;
        }

        var index = DutyRotationCalculator.GetGroupIndex(rotation, groups.Count, DateTime.Today);
        var group = groups[index - 1];
        CurrentGroupText.Text =
            $"当前应值小组（{DateTime.Today:yyyy-MM-dd}）：{DutyRotationCalculator.GroupName(group, index)}　{group.MembersText}";
    }

    private void ButtonParse_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshGroups();
        UpdateCurrentGroupText();
        var count = GroupsList.Items.Count;
        if (count == 0)
        {
            MessageBox.Show("没有解析出任何小组，请检查每行格式，例如：\n第一组：张三、李四、王五",
                "值日小组轮换", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ButtonImport_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择值日生表",
            Filter = "值日生表(*.txt;*.csv)|*.txt;*.csv|所有文件|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            RosterTextBox.Text = ReadTextFile(dialog.FileName);
            RefreshGroups();
            UpdateCurrentGroupText();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"读取文件失败：{ex.Message}", "值日小组轮换", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ButtonSaveRotation_OnClick(object sender, RoutedEventArgs e) => SaveAll(requireValidRotation: true);

    private void ButtonSave_OnClick(object sender, RoutedEventArgs e) => SaveAll(requireValidRotation: false);

    private void SaveAll(bool requireValidRotation)
    {
        _store.Groups = DutyRosterParser.Parse(RosterTextBox.Text);

        if (TryBuildRotation(out var rotation, out var error))
        {
            _store.Rotation = rotation;
        }
        else if (requireValidRotation || RotationEnabledCheck.IsChecked == true)
        {
            MessageBox.Show(error, "值日小组轮换", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_store.Rotation.Enabled && _store.Groups.Count == 0)
        {
            MessageBox.Show("已启用轮换提醒，但值日生表还没有解析出任何小组。", "值日小组轮换",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _store.Save();
        RefreshGroups();
        RefreshRecords();
        UpdateCurrentGroupText();
        MessageBox.Show("值日小组轮换设置已保存。", "值日小组轮换", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ButtonClearRecords_OnClick(object sender, RoutedEventArgs e)
    {
        if (_store.Records.Count == 0)
        {
            return;
        }
        if (MessageBox.Show("确定要清空全部轮换记录吗？", "值日小组轮换",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
        {
            return;
        }
        _store.Records.Clear();
        _store.Save();
        RefreshRecords();
    }

    private void ButtonTestRotation_OnClick(object sender, RoutedEventArgs e)
    {
        var groups = DutyRosterParser.Parse(RosterTextBox.Text);
        if (groups.Count == 0)
        {
            MessageBox.Show("请先填写或导入值日生表，并确保能解析出小组。", "值日小组轮换",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryBuildRotation(out var rotation, out var error))
        {
            MessageBox.Show(error, "值日小组轮换", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var provider = IAppHost.GetService<DutyNotificationProvider>();
        if (provider == null)
        {
            MessageBox.Show("提醒服务尚未就绪，请重启应用后重试。", "值日小组轮换", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var index = DutyRotationCalculator.GetGroupIndex(rotation, groups.Count, DateTime.Today);
        var text = DutyRotationCalculator.BuildText(rotation, groups[index - 1], index);
        provider.ShowDutyReminder(text, rotation.Speech);
    }

    /// <summary>读取文本文件，兼容 UTF-8（含 BOM）与中文系统 Excel 导出的 GB2312/GB18030。</summary>
    private static string ReadTextFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(936).GetString(bytes);
            }
            catch (Exception)
            {
                return Encoding.UTF8.GetString(bytes);
            }
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
