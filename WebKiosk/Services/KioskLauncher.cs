using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace ClassIsland.WebKiosk.Services;

/// <summary>
/// kiosk 全屏浏览器启动器。
/// 探测本机 Chrome / Edge，以 --kiosk（全屏）+ --autoplay-policy=no-user-gesture-required
/// （放开有声自动播放限制）带参启动指定网址；使用插件专属 user-data-dir，不干扰日常浏览器
/// 的配置与已打开窗口。启动 PID 记录到配置目录，支持一键关闭；未安装支持的浏览器时
/// 返回 false，由调用方回退为系统默认浏览器普通打开。
/// </summary>
public class KioskLauncher
{
    private readonly string _profileDir;
    private readonly string _pidFile;
    private readonly ILogger<KioskLauncher>? _logger;

    /// <param name="pluginConfigFolder">插件配置目录（存放 PID 与浏览器配置）。</param>
    public KioskLauncher(string pluginConfigFolder, ILogger<KioskLauncher>? logger = null)
    {
        _profileDir = Path.Combine(pluginConfigFolder, "kiosk-profile");
        _pidFile = Path.Combine(pluginConfigFolder, "kiosk.pid");
        _logger = logger;
    }

    /// <summary>浏览器配置目录（kiosk 专用，不与日常浏览器共用）。</summary>
    public string ProfileDirectory => _profileDir;

    /// <summary>默认候选路径：Chrome 与 Edge 的典型安装位置（含 Win7 时代的 x86 路径）。</summary>
    public static IReadOnlyList<string> GetDefaultCandidates()
    {
        var chrome = Path.Combine("Google", "Chrome", "Application", "chrome.exe");
        var edge = Path.Combine("Microsoft", "Edge", "Application", "msedge.exe");
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };

        var paths = new List<string>();
        // Chrome 优先，其次 Edge；两者在 Win7 上的最后支持版本均为 109，参数语义相同。
        foreach (var root in roots)
        {
            if (!string.IsNullOrEmpty(root))
            {
                paths.Add(Path.Combine(root, chrome));
                paths.Add(Path.Combine(root, edge));
            }
        }
        return paths;
    }

    /// <summary>在候选路径里返回第一个存在的浏览器可执行文件；都没有则返回 null。纯函数，可单测。</summary>
    public static string? FindBrowserAt(IEnumerable<string> candidates)
    {
        foreach (var path in candidates)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    /// <summary>探测本机可用浏览器。</summary>
    public string? FindBrowser() => FindBrowserAt(GetDefaultCandidates());

    /// <summary>拼接 kiosk 启动参数。纯函数，可单测。</summary>
    public static string BuildArguments(string url, string profileDir) =>
        $"--kiosk " +
        $"--autoplay-policy=no-user-gesture-required " +
        $"--no-first-run " +
        $"--no-default-browser-check " +
        $"--disable-session-crashed-bubble " +
        $"--user-data-dir=\"{profileDir}\" " +
        $"\"{url}\"";

    /// <summary>
    /// 以 kiosk 全屏模式打开网页。返回 false 表示未装可用浏览器或启动失败，
    /// 调用方应回退为默认浏览器普通打开。
    /// </summary>
    public bool TryLaunchKiosk(string url, out string? browserPath)
    {
        browserPath = null;
        var trimmed = (url ?? "").Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger?.LogWarning("网址无效，无法 kiosk 启动：{Url}", trimmed);
            return false;
        }

        var exe = FindBrowser();
        if (exe == null)
        {
            _logger?.LogWarning("未检测到 Chrome/Edge，kiosk 全屏不可用。");
            return false;
        }

        try
        {
            Directory.CreateDirectory(_profileDir);

            // 关掉上一次的 kiosk 窗口，避免多次触发后窗口叠加。
            Close();

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = BuildArguments(uri.AbsoluteUri, _profileDir),
                UseShellExecute = false
            };
            var process = Process.Start(psi);
            if (process == null)
            {
                _logger?.LogWarning("kiosk 浏览器进程未能启动：{Exe}", exe);
                return false;
            }

            try
            {
                File.WriteAllText(_pidFile, process.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "记录 kiosk PID 失败（不影响本次打开）。");
            }

            browserPath = exe;
            _logger?.LogInformation("已以 kiosk 全屏模式打开：{Url}（{Exe}, PID {Pid}）",
                uri.AbsoluteUri, exe, process.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "kiosk 启动失败：{Exe}", exe);
            return false;
        }
    }

    /// <summary>
    /// 关闭本插件打开的 kiosk 窗口（按 PID 精确关闭，且校验进程名只能是 chrome/msedge，
    /// 避免误杀无关进程）。返回是否确实关闭了进程。
    /// </summary>
    public bool Close()
    {
        try
        {
            if (!File.Exists(_pidFile))
            {
                return false;
            }
            var content = File.ReadAllText(_pidFile).Trim();
            if (!int.TryParse(content, out var pid))
            {
                return false;
            }

            var killed = false;
            try
            {
                var process = Process.GetProcessById(pid);
                var name = process.ProcessName.ToLowerInvariant();
                if (name is "chrome" or "msedge" or "edge")
                {
                    process.Kill();
                    killed = true;
                    _logger?.LogInformation("已关闭 kiosk 窗口（PID {Pid}）。", pid);
                }
                else
                {
                    _logger?.LogWarning("PID {Pid} 现为 {Name}，非本插件启动的浏览器，不关闭。", pid, name);
                }
            }
            catch (ArgumentException)
            {
                // 进程已退出，视为窗口已关闭。
            }

            try
            {
                File.Delete(_pidFile);
            }
            catch
            {
                // PID 文件删除失败可忽略，下次启动会覆盖。
            }
            return killed;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "关闭 kiosk 窗口失败。");
            return false;
        }
    }
}
