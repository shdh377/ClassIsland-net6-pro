using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace IslandCaller.Helpers;

public class WindowDragHelper
{
    public ILogger<WindowDragHelper> logger = IAppHost.GetService<ILogger<WindowDragHelper>>();

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MOVE = 0xF010;
    private const int HTCAPTION = 0x0002;

    /// <summary>
    /// 开始拖动窗口（WPF 版本，使用 DragMove）
    /// </summary>
    public void DragMove(Window window)
    {
        try
        {
            window.DragMove();
        }
        catch (Exception ex)
        {
            logger.LogDebug("窗口拖动异常: {Message}", ex.Message);
        }
    }
}
