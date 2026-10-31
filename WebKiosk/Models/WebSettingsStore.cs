using System.IO;
using System.Text.Json;

namespace ClassIsland.WebKiosk.Models;

/// <summary>
/// 打开计划的持久化存储（JSON，位于插件配置目录，随安装目录保存在 D 盘）。
/// 旧版本文件为纯规则数组，读取时自动迁移为「规则 + 全局静默时段」结构。
/// </summary>
public class WebSettingsStore
{
    private readonly string _path;
    private readonly object _lock = new();

    /// <summary>打开计划。</summary>
    public List<WebRule> Rules { get; set; } = [];

    /// <summary>全局静默时段：命中时所有「课前打开」计划都不提醒、也不打开。</summary>
    public List<SilenceWindow> SilenceWindows { get; set; } = [];

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
                    Rules = JsonSerializer.Deserialize<List<WebRule>>(json) ?? [];
                    SilenceWindows = [];
                    return;
                }

                var file = JsonSerializer.Deserialize<WebSettingsFile>(json);
                if (file != null)
                {
                    Rules = file.Rules ?? [];
                    SilenceWindows = file.SilenceWindows ?? [];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebKiosk] 读取规则失败：{ex.Message}");
                Rules = [];
                SilenceWindows = [];
            }
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var file = new WebSettingsFile
                {
                    Rules = Rules,
                    SilenceWindows = SilenceWindows
                };
                var json = JsonSerializer.Serialize(file, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebKiosk] 保存规则失败：{ex.Message}");
            }
        }
    }

    /// <summary>当前时刻是否落在任一静默时段内。</summary>
    public bool IsInSilenceWindow(DateTime now)
    {
        var timeOfDay = now.TimeOfDay;
        foreach (var window in SilenceWindows)
        {
            if (window.Contains(timeOfDay))
            {
                return true;
            }
        }
        return false;
    }

    private sealed class WebSettingsFile
    {
        public List<WebRule>? Rules { get; set; }
        public List<SilenceWindow>? SilenceWindows { get; set; }
    }
}
