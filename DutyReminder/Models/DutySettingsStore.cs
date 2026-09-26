using System.IO;
using System.Text.Json;

namespace ClassIsland.DutyReminder.Models;

/// <summary>
/// 值日提醒规则的持久化存储（JSON，位于插件配置目录，随安装目录保存在 D 盘）。
/// </summary>
public class DutySettingsStore
{
    private readonly string _path;
    private readonly object _lock = new();

    public List<DutyRule> Rules { get; set; } = [];

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
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    Rules = JsonSerializer.Deserialize<List<DutyRule>>(json) ?? [];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DutyReminder] 读取规则失败：{ex.Message}");
                Rules = [];
            }
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(Rules, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DutyReminder] 保存规则失败：{ex.Message}");
            }
        }
    }
}
