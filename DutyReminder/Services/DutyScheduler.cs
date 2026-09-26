using ClassIsland.Core.Abstractions.Services;
using ClassIsland.DutyReminder.Models;
using Microsoft.Extensions.Logging;

namespace ClassIsland.DutyReminder.Services;

/// <summary>
/// 值日提醒调度器：每 2 秒轮询课表与自定义时间点，命中且未触发过的规则投递给通知提供方。
/// 同一规则同一天同一时间点只触发一次（key 防重），应用重启后同一天也不会重复打扰。
/// </summary>
public class DutyScheduler
{
    private readonly DutySettingsStore _store;
    private readonly DutyNotificationProvider _provider;
    private readonly ILessonsService _lessons;
    private readonly ILogger<DutyScheduler>? _logger;

    private System.Threading.Timer? _timer;
    private readonly HashSet<string> _firedKeys = [];
    private readonly object _sync = new();

    public DutyScheduler(DutySettingsStore store, DutyNotificationProvider provider, ILessonsService lessons,
        ILogger<DutyScheduler>? logger = null)
    {
        _store = store;
        _provider = provider;
        _lessons = lessons;
        _logger = logger;
    }

    public void Start()
    {
        if (_timer != null)
        {
            return;
        }
        _timer = new System.Threading.Timer(Tick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        _logger?.LogInformation("值日提醒调度器已启动。");
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
                else
                {
                    CheckTimedRule(rule);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "值日提醒调度轮询失败。");
        }
    }

    private void CheckClassRule(DutyRule rule)
    {
        var left = _lessons.OnClassLeftTime;
        if (left <= TimeSpan.Zero || left > TimeSpan.FromMinutes(rule.LeadMinutes))
        {
            return;
        }

        var nextClass = _lessons.NextClassTimeLayoutItem;
        var key = $"class|{rule.Id}|{DateTime.Now:yyyyMMdd}|{nextClass.StartSecond:HHmmss}";
        if (TryFire(key))
        {
            _logger?.LogDebug("触发课前提醒：{Text}（距上课 {Left}）", rule.Text, left);
            _provider.ShowDutyReminder(rule.Text, rule.Speech);
        }
    }

    private void CheckTimedRule(DutyRule rule)
    {
        var now = DateTime.Now;
        if (rule.TriggerType == "weekly" && now.DayOfWeek != (DayOfWeek)rule.Weekday)
        {
            return;
        }
        if (!TimeSpan.TryParse(rule.Time, out var timeOfDay))
        {
            return;
        }

        var scheduled = DateTime.Today + timeOfDay;
        var deltaSeconds = (now - scheduled).TotalSeconds;
        // 只在计划时刻后 60 秒内触发：既容忍轮询抖动，也避免应用晚启动时补打扰。
        if (deltaSeconds < 0 || deltaSeconds > 60)
        {
            return;
        }

        var key = $"time|{rule.Id}|{DateTime.Now:yyyyMMdd}";
        if (TryFire(key))
        {
            _logger?.LogDebug("触发定时提醒：{Text}（{Time}）", rule.Text, rule.Time);
            _provider.ShowDutyReminder(rule.Text, rule.Speech);
        }
    }

    private bool TryFire(string key)
    {
        lock (_sync)
        {
            return _firedKeys.Add(key);
        }
    }
}
