using System.Windows;
using System.Windows.Threading;
using ClassIsland.WebOpener.Models;

namespace ClassIsland.WebOpener.Views;

/// <summary>
/// 打开网页前的独立确认对话框。
/// 用户点「暂不打开」→ 取消本次计划；点「打开」或 60 秒超时 → 照常打开。
/// </summary>
public partial class ConfirmDialog : Window
{
    /// <summary>用户明确要求立即打开。</summary>
    public bool OpenNowRequested { get; private set; }

    /// <summary>用户点击「暂不打开」取消本次计划。</summary>
    public bool Cancelled { get; private set; }

    private readonly DispatcherTimer _timeoutTimer;

    public ConfirmDialog(string triggerLabel, string target, bool isFile = false)
    {
        InitializeComponent();
        var kind = isFile ? "文件" : "网页";
        TitleText.Text = $"即将打开{kind}（{triggerLabel}）";
        MessageText.Text = $"1 分钟后将按计划打开以下{kind}。如需取消本次打开计划，请点击「暂不打开」。";
        UrlText.Text = target;
        if (isFile && WebFileHelper.IsImageFile(target))
        {
            MessageText.Text = "1 分钟后将按计划全屏显示以下图片。如需取消本次计划，请点击「暂不打开」。";
        }

        _timeoutTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(60)
        };
        _timeoutTimer.Tick += (_, _) =>
        {
            _timeoutTimer.Stop();
            Close(); // 超时未操作 → 视为确认，照常打开
        };
        Loaded += (_, _) => _timeoutTimer.Start();
        Closed += (_, _) => _timeoutTimer.Stop();
    }

    private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
    {
        Cancelled = true;
        Close();
    }

    private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
    {
        OpenNowRequested = true;
        Close();
    }
}
