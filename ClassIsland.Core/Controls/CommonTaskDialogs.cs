using System.Windows;
using ClassIsland.Core.Enums;
using MaterialDesignThemes.Wpf;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 一些常用的对话框工具方法
/// </summary>
public static class CommonTaskDialogs
{
    /// <summary>
    /// 显示基本提示框
    /// </summary>
    /// <param name="header">对话框标题</param>
    /// <param name="content">要显示的内容</param>
    /// <param name="owner">父窗口</param>
    public static async Task ShowDialog(string header, string content, Window? owner = null)
    {
        var builder = new CommonDialogBuilder()
            .SetCaption(header)
            .SetContent(content)
            .SetIconKind(CommonDialogIconKind.Information)
            .AddConfirmAction();

        var dialog = builder.Build();
        dialog.Owner = owner ?? Application.Current?.MainWindow;
        dialog.ShowDialog();
    }
}
