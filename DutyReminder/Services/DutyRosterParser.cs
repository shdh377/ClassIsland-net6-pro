using System.Text.RegularExpressions;
using ClassIsland.DutyReminder.Models;

namespace ClassIsland.DutyReminder.Services;

/// <summary>
/// 值日生表解析器。
/// 支持的文本格式（每组一行）：
/// <code>
/// 第一组：张三、李四、王五
/// 第二组：赵六, 钱七 孙八
/// 张三 李四 王五          ← 未写组名时自动命名为「第 N 组」
/// </code>
/// 以 <c>#</c> 开头的行会被忽略；含「组员 / 成员 / 名单」的表头行会被忽略。
/// </summary>
public static class DutyRosterParser
{
    private static readonly char[] MemberSeparators =
        [',', '，', '、', ';', '；', '|', '｜', '\t', ' '];

    private static readonly Regex GroupToken =
        new(@"^第?[0-9A-Za-z一二三四五六七八九十百零]{1,4}组$", RegexOptions.Compiled);

    private static readonly string[] HeaderKeywords = ["组员", "成员", "名单"];

    /// <summary>解析值日生表文本，返回小组列表；无法解析的行会被跳过。</summary>
    public static List<DutyGroup> Parse(string? text)
    {
        var groups = new List<DutyGroup>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return groups;
        }

        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim().TrimStart('\uFEFF').Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }
            if (HeaderKeywords.Any(k => line.Contains(k, StringComparison.Ordinal)))
            {
                continue;
            }

            var group = ParseLine(line, groups.Count + 1);
            if (group != null)
            {
                groups.Add(group);
            }
        }

        return groups;
    }

    private static DutyGroup? ParseLine(string line, int autoIndex)
    {
        string name;
        string memberPart;

        var colonIndex = IndexOfAny(line, '：', ':');
        if (colonIndex > 0 && colonIndex < line.Length - 1)
        {
            name = line[..colonIndex].Trim();
            memberPart = line[(colonIndex + 1)..].Trim();
        }
        else
        {
            // 没有冒号时，若首个分隔片段看起来就是组名（如「第一组 张三 李四」），把它当组名。
            var cells = line.Split(MemberSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (cells.Length >= 2 && GroupToken.IsMatch(cells[0].Trim()))
            {
                name = cells[0].Trim();
                memberPart = string.Join(" ", cells.Skip(1));
            }
            else
            {
                name = "";
                memberPart = line;
            }
        }

        var members = memberPart
            .Split(MemberSeparators, StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim())
            .Where(m => m.Length > 0)
            .ToList();

        if (members.Count == 0)
        {
            return null;
        }

        return new DutyGroup
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"第{autoIndex}组" : name,
            Members = members
        };
    }

    private static int IndexOfAny(string text, char a, char b)
    {
        var ia = text.IndexOf(a);
        var ib = text.IndexOf(b);
        if (ia < 0)
        {
            return ib;
        }
        if (ib < 0)
        {
            return ia;
        }
        return Math.Min(ia, ib);
    }
}
