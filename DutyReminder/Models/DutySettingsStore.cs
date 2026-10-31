using System.IO;
using System.Text.Json;

namespace ClassIsland.DutyReminder.Models;

/// <summary>
/// 值日提醒配置的持久化存储（JSON，位于插件配置目录，随安装目录保存在 D 盘）。
/// 旧版本文件为纯规则数组，读取时自动迁移为「规则 + 值日小组 + 轮换设置 + 轮换记录」结构。
/// </summary>
public class DutySettingsStore
{
    /// <summary>轮换记录最多保留条数。</summary>
    public const int MaxRecords = 200;

    private readonly string _path;
    private readonly object _lock = new();

    /// <summary>值日提醒规则。</summary>
    public List<DutyRule> Rules { get; set; } = [];

    /// <summary>值日生表：由用户上传的名单解析得到的小组列表。</summary>
    public List<DutyGroup> Groups { get; set; } = [];

    /// <summary>轮换设置。</summary>
    public DutyRotationSettings Rotation { get; set; } = new();

    /// <summary>轮换记录（新的在前）。</summary>
    public List<DutyRotationRecord> Records { get; set; } = [];

    public DutySettingsStore(string pluginConfigFolder)
    {
        Directory.CreateDirectory(pluginConfigFolder);
        _path = Path.Combine(pluginConfigFolder, "rules.json");
    }

    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_path))
                {
                    return;
                }

                var json = File.ReadAllText(_path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                // 旧版本文件是裸数组 [...]，新版本是对象 {...}。
                if (json.TrimStart().StartsWith('['))
                {
                    Rules = JsonSerializer.Deserialize<List<DutyRule>>(json) ?? [];
                    return;
                }

                var file = JsonSerializer.Deserialize<DutySettingsFile>(json);
                if (file != null)
                {
                    Rules = file.Rules ?? [];
                    Groups = file.Groups ?? [];
                    Rotation = file.Rotation ?? new DutyRotationSettings();
                    Records = file.Records ?? [];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DutyReminder] 读取配置失败：{ex.Message}");
                Rules = [];
                Groups = [];
                Records = [];
            }
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var file = new DutySettingsFile
                {
                    Rules = Rules,
                    Groups = Groups,
                    Rotation = Rotation,
                    Records = Records
                };
                var json = JsonSerializer.Serialize(file, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DutyReminder] 保存配置失败：{ex.Message}");
            }
        }
    }

    /// <summary>追加一条轮换记录，并裁剪到最大保留条数（新的在前）。</summary>
    public void AddRecord(DutyRotationRecord record)
    {
        lock (_lock)
        {
            Records.Insert(0, record);
            if (Records.Count > MaxRecords)
            {
                Records.RemoveRange(MaxRecords, Records.Count - MaxRecords);
            }
        }
    }

    private sealed class DutySettingsFile
    {
        public List<DutyRule>? Rules { get; set; }
        public List<DutyGroup>? Groups { get; set; }
        public DutyRotationSettings? Rotation { get; set; }
        public List<DutyRotationRecord>? Records { get; set; }
    }
}
