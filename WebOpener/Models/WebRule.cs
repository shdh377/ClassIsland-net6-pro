using System.Text.Json.Serialization;

namespace ClassIsland.WebOpener.Models;

/// <summary>
/// 定时打开网页规则。
/// </summary>
public class WebRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 触发类型：class（课前打开）/ weekly（每周定时）。
    /// </summary>
    public string TriggerType { get; set; } = "class";

    /// <summary>
    /// 科目名称关键字（仅 TriggerType=class），如「语文」。
    /// </summary>
    public string SubjectFilter { get; set; } = "语文";

    /// <summary>
    /// 课前提前分钟数（仅 TriggerType=class），默认 1。
    /// </summary>
    public int OpenLeadMinutes { get; set; } = 1;

    /// <summary>
    /// 打开时刻 HH:mm（仅 TriggerType=weekly）。
    /// </summary>
    public string Time { get; set; } = "14:00";

    /// <summary>
    /// 星期（仅 TriggerType=weekly），0=周日 … 6=周六。
    /// </summary>
    public int Weekday { get; set; } = 0;

    /// <summary>
    /// 要打开的网页地址。
    /// </summary>
    public string Url { get; set; } = "https://";

    [JsonIgnore]
    public string TriggerLabel => TriggerType switch
    {
        "class" => $"{SubjectFilter}课前 {OpenLeadMinutes} 分钟",
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
        $"{(Enabled ? "" : "【已停用】")}{TriggerLabel} → {Url}";
}
