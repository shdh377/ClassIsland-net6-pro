using System.Text.Json.Serialization;

namespace ClassIsland.DutyReminder.Models;

/// <summary>
/// 值日小组自动轮换设置。
/// 轮换以「基准日期 + 基准组序号」为锚点：从基准日期所在的那一周（或那一天）起算为基准组，
/// 之后按每周 / 每日顺次轮换；到点后列出新一组组员名单并提醒。
/// </summary>
public class DutyRotationSettings
{
    /// <summary>是否启用值日小组自动轮换提醒。</summary>
    public bool Enabled { get; set; }

    /// <summary>轮换周期：weekly（每周）/ daily（每日）。</summary>
    public string Interval { get; set; } = "weekly";

    /// <summary>提醒的星期（仅 Interval=weekly），0=周日 … 6=周六。</summary>
    public int Weekday { get; set; } = 1;

    /// <summary>提醒时刻 HH:mm。</summary>
    public string Time { get; set; } = "07:40";

    /// <summary>轮换基准日期 yyyy-MM-dd：该日期所在的周期视为第 AnchorGroupIndex 组。</summary>
    public string AnchorDate { get; set; } = "";

    /// <summary>基准日期对应的组序号（从 1 开始）。</summary>
    public int AnchorGroupIndex { get; set; } = 1;

    /// <summary>提醒文案模板，占位符：{group} 组名、{members} 组员、{index} 组序号。</summary>
    public string TextTemplate { get; set; } = "本周值日小组：{group}（{members}）";

    /// <summary>是否语音播报。</summary>
    public bool Speech { get; set; } = true;

    [JsonIgnore]
    public bool IsDaily => string.Equals(Interval, "daily", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string WeekdayLabel => Weekday switch
    {
        0 => "周日",
        1 => "周一",
        2 => "周二",
        3 => "周三",
        4 => "周四",
        5 => "周五",
        6 => "周六",
        _ => "周？"
    };

    [JsonIgnore]
    public string TriggerLabel => IsDaily ? $"每天 {Time} 轮换" : $"每{WeekdayLabel} {Time} 轮换";

    /// <summary>解析基准日期；无效时回退为今天。</summary>
    public DateTime ResolveAnchorDate()
    {
        if (DateTime.TryParse(AnchorDate, out var date))
        {
            return date.Date;
        }
        return DateTime.Today;
    }

    public override string ToString() => TriggerLabel;
}
