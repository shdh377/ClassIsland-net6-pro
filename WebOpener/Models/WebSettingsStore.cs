using System.IO;
using System.Text.Json;

namespace ClassIsland.WebOpener.Models;

/// <summary>
/// 打开网页规则的持久化存储（JSON，位于插件配置目录，随安装目录保存在 D 盘）。
/// </summary>
public class WebSettingsStore
{
    private readonly string _path;
    private readonly object _lock = new();

    public List<WebRule> Rules { get; set; } = [];

    public WebSettingsStore(string pluginConfigFolder)
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
                    Rules = JsonSerializer.Deserialize<List<WebRule>>(json) ?? [];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebOpener] 读取规则失败：{ex.Message}");
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
                Console.WriteLine($"[WebOpener] 保存规则失败：{ex.Message}");
            }
        }
    }
}
