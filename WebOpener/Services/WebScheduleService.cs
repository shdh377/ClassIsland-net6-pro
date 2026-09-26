using System.Diagnostics;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.WebOpener.Models;
using ClassIsland.WebOpener.Views;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace ClassIsland.WebOpener.Services;

/// <summary>
/// 定时打开网页调度器。
/// 每 2 秒轮询：课前规则按「科目匹配 + 距上课倒计时」判断，每周规则按本机时间判断。
/// 打开前 1 分钟（提醒提前量 = 打开提前量 + 1 分钟）弹独立确认对话框；
/// 槽位 key 防重 + 取消集合保证同一计划只处理一次，重启后同一天也不会重复打扰。
/// </summary>
public class WebScheduleService
{
    private const int RemindBeforeOpenMinutes = 1;

    private readonly WebSettingsStore _store;
    private readonly ILessonsService _lessons;
    private readonly WebOpenerNotificationProvider _bannerProvider;
    private readonly ILogger<WebScheduleService>? _logger;

    private System.Threading.Timer? _timer;
    private readonly HashSet<string> _firedKeys = [];
    private readonly HashSet<string> _cancelledOpenKeys = [];
    private readonly object _sync = new();

    public WebScheduleService(WebSettingsStore store, ILessonsService lessons,
        WebOpenerNotificationProvider bannerProvider, ILogger<WebScheduleService>? logger = null)
    {
        _store = store;
        _lessons = lessons;
        _bannerProvider = bannerProvider;
        _logger = logger;
    }

    public void Start()
    {
        if (_timer != null)
        {
            return;
        }
        _timer = new System.Threading.Timer(Tick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        _logger?.LogInformation("定时打开网页调度器已启动。");
    }

    private void Tick(object? state)
    {
        try
        {
            foreach (var rule in _store.Rules)
            {
                if (!rule.Enabled)
                {
                    continue;
                }
                if (rule.TriggerType == "class")
                {
                    CheckClassRule(rule);
                }
                else if (rule.TriggerType == "weekly")
                {
                    CheckWeeklyRule(rule);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "定时打开网页调度轮询失败。");
        }
    }

    private void CheckClassRule(WebRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.SubjectFilter))
        {
            return;
        }

        var subjectName = _lessons.NextClassSubject?.Name ?? "";
        if (!subjectName.Contains(rule.SubjectFilter, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var left = _lessons.OnClassLeftTime;
        if (left <= TimeSpan.Zero)
        {
            return;
        }

        var nextClass = _lessons.NextClassTimeLayoutItem;
        var baseKey = $"c|{rule.Id}|{nextClass.StartSecond:yyyyMMddHHmmss}";

        // 提醒：打开前 1 分钟（即课前 OpenLeadMinutes + 1 分钟）
        var remindLead = rule.OpenLeadMinutes + RemindBeforeOpenMinutes;
        if (left <= TimeSpan.FromMinutes(remindLead))
        {
            if (TryMark($"{baseKey}|remind"))
            {
                ShowConfirmDialog(rule, openKey: $"{baseKey}|open");
            }
        }

        // 打开：课前 OpenLeadMinutes
        if (left <= TimeSpan.FromMinutes(rule.OpenLeadMinutes))
        {
            RequestOpen(rule, $"{baseKey}|open");
        }
    }

    private void CheckWeeklyRule(WebRule rule)
    {
        var now = DateTime.Now;
        if (now.DayOfWeek != (DayOfWeek)rule.Weekday)
        {
            return;
        }
        if (!TimeSpan.TryParse(rule.Time, out var timeOfDay))
        {
            return;
        }

        var openAt = DateTime.Today + timeOfDay;
        var remindAt = openAt - TimeSpan.FromMinutes(RemindBeforeOpenMinutes);
        var dayKey = $"{DateTime.Now:yyyyMMdd}";

        var remindDelta = (now - remindAt).TotalSeconds;
        if (remindDelta >= 0 && remindDelta <= 60)
        {
            if (TryMark($"w|{rule.Id}|{dayKey}|remind"))
            {
                ShowConfirmDialog(rule, openKey: $"w|{rule.Id}|{dayKey}|open");
            }
        }

        var openDelta = (now - openAt).TotalSeconds;
        if (openDelta >= 0 && openDelta <= 60)
        {
            RequestOpen(rule, $"w|{rule.Id}|{dayKey}|open");
        }
    }

    private void RequestOpen(WebRule rule, string openKey)
    {
        lock (_sync)
        {
            if (_cancelledOpenKeys.Contains(openKey))
            {
                return;
            }
            if (!_firedKeys.Add(openKey))
            {
                return;
            }
        }
        OpenUrl(rule);
    }

    private void ShowConfirmDialog(WebRule rule, string openKey)
    {
        // 先走 ClassIsland 通知系统弹横幅提醒，再弹独立确认对话框。
        ShowBanner();
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                var dialog = new ConfirmDialog(rule.TriggerLabel, rule.Url);
                dialog.Closed += (_, _) =>
                {
                    if (dialog.Cancelled)
                    {
                        lock (_sync)
                        {
                            _cancelledOpenKeys.Add(openKey);
                        }
                        _logger?.LogDebug("用户取消了打开网页计划：{Url}", rule.Url);
                    }
                    else if (dialog.OpenNowRequested)
                    {
                        // 用户主动点「打开」：立即打开并占住槽位，到点不再重复打开。
                        RequestOpen(rule, openKey);
                    }
                };
                dialog.Show();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "弹出确认对话框失败，将按计划直接打开。");
            }
        });
    }

    private void ShowBanner()
    {
        try
        {
            // 横幅不显示网址，仅提示即将打开。
            _bannerProvider.ShowOpenWebReminder("1 分钟后将打开网页");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "横幅提醒弹出失败。");
        }
    }

    /// <summary>设置页「测试触发」：与到点时一致的横幅 + 确认对话框，不写入计划。</summary>
    public void TestReminder(string triggerLabel, string url)
    {
        ShowBanner();
        Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                var dialog = new ConfirmDialog("测试 · " + triggerLabel, url);
                dialog.Closed += (_, _) =>
                {
                    if (dialog.OpenNowRequested)
                    {
                        OpenUrlDirect(url, "测试");
                    }
                };
                dialog.Show();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "弹出测试对话框失败。");
            }
        });
    }

    private void OpenUrl(WebRule rule)
    {
        OpenUrlDirect(rule.Url, rule.TriggerLabel);
    }

    /// <summary>校验并打开网页（设置页「测试触发」与调度共用同一条打开链路）。</summary>
    public void OpenUrlDirect(string url, string? ruleLabel = null)
    {
        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger?.LogWarning("网址无效，已跳过：{Url}", trimmed);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
            _logger?.LogInformation("已打开网页：{Url}（{Rule}）", uri.AbsoluteUri, ruleLabel ?? "测试");
        }
        catch (Exception ex)
        {
            // 与宿主「运行」行动一致的兜底方式
            try
            {
                Process.Start("explorer.exe", uri.AbsoluteUri);
            }
            catch (Exception fallbackEx)
            {
                _logger?.LogError(fallbackEx, "打开网页失败：{Url}", uri.AbsoluteUri);
            }
        }
    }

    private bool TryMark(string key)
    {
        lock (_sync)
        {
            return _firedKeys.Add(key);
        }
    }
}
