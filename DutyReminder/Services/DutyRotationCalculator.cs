using ClassIsland.DutyReminder.Models;

namespace ClassIsland.DutyReminder.Services;

/// <summary>
/// 值日小组轮换算法：由「基准日期 + 基准组序号 + 轮换周期」推算出任意日期所属的小组。
/// 每周轮换以自然周（周一为一周之始）对齐；每日轮换按天顺次推进。
/// </summary>
public static class DutyRotationCalculator
{
    /// <summary>取某日期所属轮换周期的起始日（每周 = 该周周一；每日 = 当天）。</summary>
    public static DateTime GetCycleStart(DutyRotationSettings settings, DateTime date)
    {
        var day = date.Date;
        if (settings.IsDaily)
        {
            return day;
        }
        // DayOfWeek：周日=0 … 周六=6 → 换算成「距本周周一的天数」
        var offset = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-offset);
    }

    /// <summary>指定日期所属的组序号（从 1 开始）；没有任何小组时返回 0。</summary>
    public static int GetGroupIndex(DutyRotationSettings settings, int groupCount, DateTime date)
    {
        if (groupCount <= 0)
        {
            return 0;
        }

        var anchorCycle = GetCycleStart(settings, settings.ResolveAnchorDate());
        var targetCycle = GetCycleStart(settings, date);
        var days = (targetCycle - anchorCycle).Days;
        var periods = settings.IsDaily ? days : (int)Math.Floor(days / 7.0);

        var anchorIndex = Math.Clamp(settings.AnchorGroupIndex, 1, groupCount);
        // C# 取模对负数返回负值，这里补齐为正后再换算。
        return ((anchorIndex - 1 + periods) % groupCount + groupCount) % groupCount + 1;
    }

    /// <summary>指定日期应当值的小组；无法确定时返回 null。</summary>
    public static DutyGroup? GetGroupAt(IReadOnlyList<DutyGroup> groups, DutyRotationSettings settings, DateTime date)
    {
        var index = GetGroupIndex(settings, groups.Count, date);
        return index >= 1 && index <= groups.Count ? groups[index - 1] : null;
    }

    /// <summary>轮换周期的去重 key（同一周期内只提醒一次）。</summary>
    public static string GetCycleKey(DutyRotationSettings settings, DateTime date) =>
        $"rotation|{GetCycleStart(settings, date):yyyyMMdd}";

    /// <summary>按模板生成提醒文案。</summary>
    public static string BuildText(DutyRotationSettings settings, DutyGroup group, int index)
    {
        var name = string.IsNullOrWhiteSpace(group.Name) ? $"第{index}组" : group.Name;
        var template = string.IsNullOrWhiteSpace(settings.TextTemplate)
            ? "值日小组：{group}（{members}）"
            : settings.TextTemplate;
        return template
            .Replace("{group}", name)
            .Replace("{members}", group.MembersText)
            .Replace("{index}", index.ToString());
    }

    /// <summary>组名（未命名时回退为「第 N 组」）。</summary>
    public static string GroupName(DutyGroup group, int index) =>
        string.IsNullOrWhiteSpace(group.Name) ? $"第{index}组" : group.Name;
}
