using System.IO;

namespace IslandCaller.Shared;

/// <summary>
/// IslandCaller 数据目录定位。
/// 所有运行数据（名单、历史、设置）统一存放在应用根目录的 Data\IslandCaller 下，
/// 以便在带还原系统的生产环境中持久保留（不依赖 %AppData% 与注册表）。
/// </summary>
public static class AppPaths
{
    /// <summary>IslandCaller 数据根目录：&lt;应用根目录&gt;\Data\IslandCaller</summary>
    public static string DataRoot { get; set; } = "";

    /// <summary>名单档案目录</summary>
    public static string ProfileFolder => Path.Combine(DataRoot, "Profile");

    /// <summary>点名历史目录</summary>
    public static string HistoryFolder => Path.Combine(DataRoot, "History");

    /// <summary>插件设置文件路径</summary>
    public static string SettingsPath => Path.Combine(DataRoot, "Settings.json");
}
