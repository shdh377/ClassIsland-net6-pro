using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ClassIsland.WebOpener.Views;

/// <summary>
/// 图片全屏显示窗口：置顶、铺满主屏，可点击或按 Esc 关闭；
/// 配置了显示时长时到点自动关闭。
/// </summary>
public partial class ImageFullscreenWindow : Window
{
    private readonly DispatcherTimer? _closeTimer;

    private ImageFullscreenWindow(ImageSource image, string fileName, int displaySeconds)
    {
        InitializeComponent();
        DisplayImage.Source = image;
        HintText.Text = displaySeconds > 0
            ? $"{fileName}　·　{displaySeconds} 秒后自动关闭，点击或按 Esc 可立即关闭"
            : $"{fileName}　·　点击或按 Esc 关闭";

        if (displaySeconds > 0)
        {
            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(displaySeconds)
            };
            _closeTimer.Tick += (_, _) =>
            {
                _closeTimer.Stop();
                Close();
            };
        }

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };
        MouseLeftButtonDown += (_, _) => Close();
        Closed += (_, _) => _closeTimer?.Stop();

        Loaded += (_, _) => _closeTimer?.Start();
    }

    /// <summary>
    /// 尝试全屏显示图片。返回 false 表示图片无法解码（调用方应回退为系统默认程序打开）。
    /// 必须在 UI 线程调用。
    /// </summary>
    public static bool TryShow(string imagePath, int displaySeconds)
    {
        ImageSource image;
        try
        {
            image = LoadImage(imagePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebOpener] 读取图片失败，将改用默认程序打开：{ex.Message}");
            return false;
        }

        var window = new ImageFullscreenWindow(image, Path.GetFileName(imagePath), displaySeconds);
        window.Show();
        return true;
    }

    private static BitmapImage LoadImage(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        // OnLoad：读入后立即释放文件句柄，避免占用原图；IgnoreImageCache 保证重新读取最新文件。
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bitmap.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
