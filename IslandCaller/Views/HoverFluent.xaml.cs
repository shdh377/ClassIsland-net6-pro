using ClassIsland.Shared;
using IslandCaller.Helpers;
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

    public HoverFluent()
    {
        InitializeComponent();
        vm = DataContext as HoverFluentViewModel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        windowTopmostHelper.EnsureNoActivate(this);
        StartTopmostLoop();
        ApplyTopmost("窗口打开");
        logger.LogInformation("HoverFluent 悬浮窗初始化成功");
    }

    protected override void OnClosed(EventArgs e)
    {
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
