using ClassIsland.Shared;
using IslandCaller.Helpers;
using IslandCaller.Models;
using IslandCaller.Services.IslandCallerService;
using IslandCaller.ViewModels;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Input;

namespace IslandCaller.Views;

public partial class HoverFluent : Window
{
    private HoverFluentViewModel vm;
    private readonly ILogger<HoverFluent> logger = IAppHost.GetService<ILogger<HoverFluent>>();
    private readonly WindowTopmostHelper windowTopmostHelper = IAppHost.GetService<WindowTopmostHelper>();
    private readonly IslandCallerService islandCallerService = IAppHost.GetService<IslandCallerService>();
    private CancellationTokenSource? topmostCts;

    // Button2 拖拽状态
    private bool _isDragging;
    private Point _dragStartPoint;
    private const double DragThreshold = 4; // 移动超过4px视为拖拽

    // 基准尺寸：Call 按钮 105x70(1.5:1) + ... 按钮 70x70(1:1 正方形) + 间隔 10 => 窗口宽 185、高 70
    private const double BaseButton1Width = 105;
    private const double BaseButton2Width = 70;
    private const double BaseButtonHeight = 70;
    private const double BaseGap = 10;

    public HoverFluent()
    {
        InitializeComponent();
        vm = DataContext as HoverFluentViewModel;
        ApplyWindowScale();
        // 悬浮窗缩放系数变化时，整体调整窗口大小（含容器同步），字号随缩放变化
        Settings.Instance.Hover.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Settings.Instance.Hover.ScalingFactor))
            {
                Dispatcher.Invoke(ApplyWindowScale);
            }
        };
    }

    /// <summary>根据缩放系数设置窗口整体尺寸（Call 1.5:1 + 间隔 + ... 1:1，随缩放等比）</summary>
    private void ApplyWindowScale()
    {
        var scale = Settings.Instance.Hover.ScalingFactor;
        Width = (BaseButton1Width + BaseGap + BaseButton2Width) * scale;
        Height = BaseButtonHeight * scale;
        if (vm != null)
        {
            vm.Button1Width = BaseButton1Width * scale;
            vm.Button2Width = BaseButton2Width * scale;
            vm.ButtonHeight = BaseButtonHeight * scale;
            vm.Gap = BaseGap * scale;
            vm.GridWidth = (BaseButton1Width + BaseGap + BaseButton2Width) * scale;
            vm.GridHeight = BaseButtonHeight * scale;
            vm.FontSize = 22 * scale;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        windowTopmostHelper.EnsureNoActivate(this);
        StartTopmostLoop();
        ApplyTopmost("窗口打开");
        // 窗口初始化后再应用一次缩放，确保宽高都正确生效
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(ApplyWindowScale));
        logger.LogInformation("HoverFluent 悬浮窗初始化成功");
    }

    protected override void OnClosed(EventArgs e)
    {
        // 关闭时保存窗口位置（拖拽移动后持久化）
        try
        {
            Settings.Instance.Hover.Position.X = Left;
            Settings.Instance.Hover.Position.Y = Top;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "保存悬浮窗位置失败");
        }
        topmostCts?.Cancel();
        topmostCts?.Dispose();
        topmostCts = null;
        base.OnClosed(e);
    }

    private void StartTopmostLoop()
    {
        topmostCts?.Cancel();
        topmostCts?.Dispose();
        topmostCts = new CancellationTokenSource();
        var token = topmostCts.Token;

        Task.Run(async () =>
        {
            logger.LogInformation("HoverFluent 置顶任务启动，间隔: 3000ms");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(3000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (token.IsCancellationRequested) break;

                Dispatcher.Invoke(() => ApplyTopmost("定时器触发"));
            }
            logger.LogInformation("HoverFluent 置顶任务结束");
        }, token);
    }

    private void ApplyTopmost(string reason)
    {
        windowTopmostHelper.EnsureTopmost(this);
        Focusable = false;
    }

    private void Button2_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;
        Button2.CaptureMouse();
    }

    private void Button2_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var currentPoint = e.GetPosition(this);
        var diff = currentPoint - _dragStartPoint;

        if (!_isDragging && (Math.Abs(diff.X) > DragThreshold || Math.Abs(diff.Y) > DragThreshold))
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            Left += diff.X;
            Top += diff.Y;
        }
    }

    private void Button2_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        Button2.ReleaseMouseCapture();

        if (!_isDragging)
        {
            // 没有拖拽，视为单击 → 打开自定义抽取
            var personalCall = new PersonalCall();
            personalCall.Owner = this;
            personalCall.Show();
        }

        _isDragging = false;
    }

    private void Button1_Click(object sender, RoutedEventArgs e)
    {
        islandCallerService.ShowRandomStudent(1);
    }
}
