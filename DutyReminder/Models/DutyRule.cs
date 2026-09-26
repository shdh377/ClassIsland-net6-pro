using System.Text.Json.Serialization;

namespace ClassIsland.DutyReminder.Models;

/// <summary>
/// 值日提醒规则。
/// </summary>
public class DutyRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 触发类型：class（每节上课前）/ daily（每天固定时间）/ weekly（每周固定时间）。
    /// </summary>
    public string TriggerType { get; set; } = "class";

    /// <summary>
    /// 每节上课前提前的分钟数（仅 TriggerType=class）。
    /// </summary>
    public int LeadMinutes { get; set; } = 2;

    /// <summary>
    /// 触发时间 HH:mm（仅 TriggerType=daily/weekly）。
    /// </summary>
    public string Time { get; set; } = "07:40";

    /// <summary>
    /// 星期（仅 TriggerType=weekly），0=周日 … 6=周六，与 DayOfWeek 一致。
    /// </summary>
    public int Weekday { get; set; } = 1;

    /// <summary>
    /// 提醒文案。
    /// </summary>
    public string Text { get; set; } = "请值日生擦黑板";

    /// <summary>
    /// 是否语音播报。
    /// </summary>
    public bool Speech { get; set; } = true;

    [JsonIgnore]
    public string TriggerLabel => TriggerType switch
    {
        "class" => $"课前 {LeadMinutes} 分钟",
        "daily" => $"每天 {Time}",
        "weekly" => $"每{WeekdayLabel} {Time}",
        _ => TriggerType
    };

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

    public override string ToString() =>
        $"{(Enabled ? "" : "【已停用】")}{TriggerLabel} · {Text}";
}
