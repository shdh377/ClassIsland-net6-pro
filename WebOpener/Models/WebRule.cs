using System.IO;
using System.Text.Json.Serialization;

namespace ClassIsland.WebOpener.Models;

/// <summary>
/// 定时打开计划。
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
    /// 打开对象类型：url（网页）/ file（本地文件，含图片）。
    /// </summary>
    public string TargetType { get; set; } = "url";

    /// <summary>
    /// 要打开的网页地址（TargetType=url）。
    /// </summary>
    public string Url { get; set; } = "https://";

    /// <summary>
    /// 要打开的本地文件路径（TargetType=file），可为图片、文档、程序等。
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// 本地文件是图片时是否全屏显示（TargetType=file）。
    /// </summary>
    public bool FullscreenImage { get; set; } = true;

    /// <summary>
    /// 图片全屏显示时长（秒），0 表示不自动关闭（点击或按 Esc 关闭）。
    /// </summary>
    public int ImageDisplaySeconds { get; set; } = 0;

    /// <summary>
    /// 到点要打开的对象（网页地址或本地文件路径）。
    /// </summary>
    [JsonIgnore]
    public string Target => IsFile ? FilePath : Url;

    [JsonIgnore]
    public bool IsFile => string.Equals(TargetType, "file", StringComparison.OrdinalIgnoreCase);

    /// <summary>目标为图片且开启了全屏时为 true。</summary>
    [JsonIgnore]
    public bool IsFullscreenImage => IsFile && FullscreenImage && WebFileHelper.IsImageFile(FilePath);

    [JsonIgnore]
    public string TriggerLabel => TriggerType switch
    {
        "class" => $"{SubjectFilter}课前 {OpenLeadMinutes} 分钟",
        "weekly" => $"每{WeekdayLabel} {Time}",
        _ => TriggerType
    };

    /// <summary>顶部列表用的完整描述。</summary>
    [JsonIgnore]
    public string DisplayLabel => $"{TriggerLabel} → {TargetLabel}";

    [JsonIgnore]
    public string TargetLabel
    {
        get
        {
            var target = Target;
            if (string.IsNullOrWhiteSpace(target))
            {
                return "（未设置）";
            }
            var name = IsFile ? Path.GetFileName(target) : target;
            return IsFullscreenImage ? $"{name}（图片·全屏）" : name;
        }
    }

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
        $"{(Enabled ? "" : "【已停用】")}{DisplayLabel}";
}
