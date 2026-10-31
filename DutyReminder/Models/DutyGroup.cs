using System.Text.Json.Serialization;

namespace ClassIsland.DutyReminder.Models;

/// <summary>
/// 值日小组：一个组名 + 若干组员。
/// </summary>
public class DutyGroup
{
    /// <summary>组名，如「第一组」。为空时显示为「第 N 组」。</summary>
    public string Name { get; set; } = "";

    /// <summary>组员姓名。</summary>
    public List<string> Members { get; set; } = [];

    /// <summary>组员用顿号连接，便于提醒文案与列表展示。</summary>
    [JsonIgnore]
    public string MembersText => string.Join("、", Members);

    [JsonIgnore]
    public string Display => string.IsNullOrWhiteSpace(Name)
        ? $"（未命名）：{MembersText}"
        : $"{Name}：{MembersText}";

    public override string ToString() => Display;
}

/// <summary>
/// 一次值日小组轮换的记录（用于「自动轮换记录」）。
/// </summary>
public class DutyRotationRecord
{
    /// <summary>记录生成时间。</summary>
    public DateTime Time { get; set; } = DateTime.Now;

    /// <summary>组序号（从 1 开始）。</summary>
    public int GroupIndex { get; set; }

    /// <summary>组名。</summary>
    public string GroupName { get; set; } = "";

    /// <summary>组员名单（顿号连接）。</summary>
    public string Members { get; set; } = "";

    /// <summary>实际播报/展示的提醒文案。</summary>
    public string Text { get; set; } = "";

    public override string ToString() =>
        $"{Time:yyyy-MM-dd HH:mm}　第{GroupIndex}组 {GroupName}　{Members}";
}
