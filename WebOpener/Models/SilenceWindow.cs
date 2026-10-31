using System.Text.Json.Serialization;

namespace ClassIsland.WebOpener.Models;

/// <summary>
/// 全局静默时段：命中该时间段的「课前打开」计划不弹出提醒、也不打开目标。
/// 支持跨零点（如 23:00–06:00）。
/// </summary>
public class SilenceWindow
{
    /// <summary>起始时刻 HH:mm。</summary>
    public string Start { get; set; } = "12:00";

    /// <summary>结束时刻 HH:mm。</summary>
    public string End { get; set; } = "13:30";

    [JsonIgnore]
    public string Label => $"{Start} – {End}";

    /// <summary>给定时刻是否落在本静默时段内（支持跨零点）。</summary>
    public bool Contains(TimeSpan timeOfDay)
    {
        if (!TryParse(Start, out var start) || !TryParse(End, out var end))
        {
            return false;
        }
        if (start == end)
        {
            return false;
        }
        if (start < end)
        {
            return timeOfDay >= start && timeOfDay < end;
        }
        // 跨零点：例如 23:00–06:00
        return timeOfDay >= start || timeOfDay < end;
    }

    public static bool TryParse(string? text, out TimeSpan value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }
        if (!TimeSpan.TryParse(text.Trim(), out var span))
        {
            return false;
        }
        if (span < TimeSpan.Zero || span >= TimeSpan.FromDays(1))
        {
            return false;
        }
        value = span;
        return true;
    }

    public override string ToString() => Label;
}
