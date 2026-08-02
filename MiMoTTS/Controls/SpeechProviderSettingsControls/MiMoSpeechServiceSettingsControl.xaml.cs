using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MiMoTTS.Models;
using MiMoTTS.Shared;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MiMoTTS.Controls.SpeechProviderSettingsControls;

public partial class MiMoSpeechServiceSettingsControl : SpeechProviderControlBase, INotifyPropertyChanged
{
    private readonly ILogger<MiMoSpeechServiceSettingsControl>? _logger;

    private DispatcherTimer? _saveDebounceTimer;

    /// <summary>
    /// 是否正在初始化加载设置。加载期间禁止触发防抖保存，
    /// 避免填充默认值或读取失败时用空数据覆盖已保存的 API Key 等配置。
    /// </summary>
    private bool _isLoading = true;

    public MiMoSpeechSettings Settings { get; set; } = new();
    public IReadOnlyList<string> ModelOptions { get; } =
    [
        MiMoSpeechSettings.ModelV2,
        MiMoSpeechSettings.ModelV25,
        MiMoSpeechSettings.ModelV25VoiceClone
    ];
    public IReadOnlyList<string> ApiBaseUrlOptions { get; } =
    [
        MiMoSpeechSettings.DefaultApiBaseUrl,
        MiMoSpeechSettings.TokenPlanApiBaseUrl
    ];
    public IReadOnlyList<string> SpeedStyleOptions { get; } =
    [
        "默认",
        "变快",
        "变慢"
    ];

    public bool IsV25Model =>
        string.Equals(Settings.Model, MiMoSpeechSettings.ModelV25, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Settings.Model, MiMoSpeechSettings.ModelV25VoiceClone, StringComparison.OrdinalIgnoreCase);

    public bool IsVoiceCloneModel =>
        string.Equals(Settings.Model, MiMoSpeechSettings.ModelV25VoiceClone, StringComparison.OrdinalIgnoreCase);

    public MiMoSpeechServiceSettingsControl()
    {
        InitializeComponent();
        try
        {
            _logger = IAppHost.GetService<ILogger<MiMoSpeechServiceSettingsControl>>();
        }
        catch
        {
            // 应用宿主尚未初始化时允许无日志运行
            _logger = null;
        }

        LoadSettings();
    }

    private static string GetConfigPath() =>
        Path.Combine(GlobalConstants.PluginConfigFolder, "Settings.json");

    /// <summary>
    /// 读取磁盘上已保存的 API Key（文件不存在或读取失败时返回 null）。
    /// </summary>
    private static string? ReadDiskApiKey(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                return null;
            }
            using var fs = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<MiMoSpeechSettings>(json)?.ApiKey;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 立即将设置写入磁盘。采用“先写临时文件再原子替换”的方式，
    /// 确保任何并发读取方（语音服务、新打开的设置控件）不会读到被截断的空文件，
    /// 从而避免因读取失败而用空数据覆盖已保存的 API Key。
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            var configDir = GlobalConstants.PluginConfigFolder;
            if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            var configPath = GetConfigPath();

            // 防误清空保护：若本控件内存中的 API Key 为空（例如控件在用户输入 Key 之前就已创建，
            // 或 PasswordBox 被程序性重置），而磁盘上已保存了非空 Key，则保留磁盘上的 Key。
            // 用户正在输入/清空 Key 时（PasswordBox 获得焦点）不受此保护限制。
            if (string.IsNullOrEmpty(Settings.ApiKey) && !ApiKeyPasswordBox.IsKeyboardFocusWithin)
            {
                var diskKey = ReadDiskApiKey(configPath);
                if (!string.IsNullOrEmpty(diskKey))
                {
                    _logger?.LogWarning(
                        "MiMoTTS 设置保存前检测到内存 API Key 为空而磁盘已有 Key，已保留磁盘值，避免误清空。路径：{Path}",
                        configPath);
                    Settings.ApiKey = diskKey;
                }
            }

            var tempPath = configPath + ".tmp";
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, configPath, true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "保存 MiMoTTS 设置失败，路径：{Path}", GetConfigPath());
        }
    }

    /// <summary>
    /// 延迟保存（防抖），避免频繁属性变化时反复写磁盘。
    /// </summary>
    private void ScheduleSave()
    {
        if (_saveDebounceTimer == null)
        {
            _saveDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _saveDebounceTimer.Tick += (_, _) =>
            {
                _saveDebounceTimer.Stop();
                SaveSettings();
            };
        }

        _saveDebounceTimer.Stop();
        _saveDebounceTimer.Start();
    }

    private void LoadSettings()
    {
        var configPath = GetConfigPath();
        if (File.Exists(configPath))
        {
            try
            {
                using var fs = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                var json = reader.ReadToEnd();
                Settings = JsonSerializer.Deserialize<MiMoSpeechSettings>(json) ?? new MiMoSpeechSettings();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取 MiMoTTS 设置失败，使用默认设置。路径：{Path}", configPath);
                Settings = new MiMoSpeechSettings();
            }
        }

        Settings.PropertyChanged += SettingsOnPropertyChanged;

        if (string.IsNullOrWhiteSpace(Settings.ApiBaseUrl) ||
            (Settings.ApiBaseUrl != MiMoSpeechSettings.DefaultApiBaseUrl &&
             Settings.ApiBaseUrl != MiMoSpeechSettings.TokenPlanApiBaseUrl))
        {
            Settings.ApiBaseUrl = MiMoSpeechSettings.DefaultApiBaseUrl;
        }

        if (string.IsNullOrWhiteSpace(Settings.Model) ||
            (Settings.Model != MiMoSpeechSettings.ModelV2 &&
             Settings.Model != MiMoSpeechSettings.ModelV25 &&
             Settings.Model != MiMoSpeechSettings.ModelV25VoiceClone))
        {
            Settings.Model = MiMoSpeechSettings.ModelV2;
        }

        if (string.IsNullOrWhiteSpace(Settings.SpeedStyle))
        {
            Settings.SpeedStyle = "默认";
        }

        // 设置 API Key 到 PasswordBox；输入时立即写盘，确保不丢失
        ApiKeyPasswordBox.Password = Settings.ApiKey;
        ApiKeyPasswordBox.PasswordChanged += (s, e) =>
        {
            // 仅当用户正在输入时才同步到 Settings 并保存；
            // 控件重建、模板重置等程序性 Password 变化不会覆盖已保存的 API Key
            if (!ApiKeyPasswordBox.IsKeyboardFocusWithin)
            {
                return;
            }
            // 值未变化时不写盘，避免无意义写入
            if (Settings.ApiKey == ApiKeyPasswordBox.Password)
            {
                return;
            }
            Settings.ApiKey = ApiKeyPasswordBox.Password;
            SaveSettings();
        };

        DataContext = this;

        // 加载与初始化全部完成，此后属性变化才会触发防抖保存
        _isLoading = false;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        // 加载初始化期间（填充默认值）不保存，避免用尚未就绪的数据覆盖磁盘配置
        if (_isLoading)
        {
            return;
        }

        if (args.PropertyName == nameof(MiMoSpeechSettings.Model))
        {
            OnPropertyChanged(nameof(IsV25Model));
            OnPropertyChanged(nameof(IsVoiceCloneModel));
        }

        // 防抖保存
        ScheduleSave();
    }

    private void BrowseAudioFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择音色克隆音频样本",
            Filter = "音频文件 (*.mp3;*.wav)|*.mp3;*.wav",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            Settings.VoiceCloneAudioPath = dialog.FileName;
        }
    }

    private void ModelSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsVoiceCloneModel));
        OnPropertyChanged(nameof(IsV25Model));
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
