using System.IO;
using System.Text.Json;
using IslandCaller.Services;
using IslandCaller.Shared;

namespace IslandCaller.Models;

public class Settings(ProfileService profileService)
{
    public static SettingsModel Instance { get; } = new SettingsModel();
    public ProfileService ProfileService { get; } = profileService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 首次启动：创建默认演示名单档案并写出默认设置文件。
    /// 设置以 JSON 形式存放在应用根目录 Data\IslandCaller\Settings.json。
    /// </summary>
    private void InitializeNewInstall()
    {
        ProfileService.CreateDemoProfile(Settings.Instance.Profile.DefaultProfile);
        ClassIsland.Core.Controls.CommonTaskDialogs.ShowDialog("Welcome", "欢迎使用IslandCaller2.0").Wait();
        Save();
    }

    public void Load()
    {
        if (!File.Exists(AppPaths.SettingsPath))
        {
            InitializeNewInstall();
        }
        else
        {
            var json = File.ReadAllText(AppPaths.SettingsPath);
            var model = JsonSerializer.Deserialize<SettingsModel>(json, JsonOptions);
            if (model != null)
            {
                Instance.General.BreakDisable = model.General.BreakDisable;
                Instance.General.Interruptable = model.General.Interruptable;
                Instance.Profile.ProfileNum = model.Profile.ProfileNum;
                Instance.Profile.DefaultProfile = model.Profile.DefaultProfile;
                Instance.Profile.IsPreferProfile = model.Profile.IsPreferProfile;
                Instance.Profile.ProfileList = model.Profile.ProfileList;
                Instance.Profile.ProfilePrefer = model.Profile.ProfilePrefer;
                Instance.Hover.IsEnable = model.Hover.IsEnable;
                Instance.Hover.ScalingFactor = model.Hover.ScalingFactor;
                Instance.Hover.Position.X = model.Hover.Position.X;
                Instance.Hover.Position.Y = model.Hover.Position.Y;
            }
            Save();
        }

        SettingsBinder.Bind(Instance, Save);
    }

    public void Save()
    {
        string path = AppPaths.SettingsPath;
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(path, JsonSerializer.Serialize(Instance, JsonOptions));
    }
}

public static class SettingsBinder
{
    public static void Bind(SettingsModel model, Action onChange)
    {
        // General
        model.General.PropertyChanged += (_, _) => onChange();

        // Hover
        model.Hover.PropertyChanged += (_, _) => onChange();
        model.Hover.Position.PropertyChanged += (_, _) => onChange();
    }
}
