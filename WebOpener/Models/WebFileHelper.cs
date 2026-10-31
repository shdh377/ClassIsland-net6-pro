using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClassIsland.WebOpener.Models;

/// <summary>
/// 本地文件相关的小工具：判断是否图片、常用图片扩展名。
/// </summary>
public static class WebFileHelper
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".ico", ".webp", ".jfif"
    };

    public static bool IsImageFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }
        var ext = Path.GetExtension(path.Trim());
        return !string.IsNullOrEmpty(ext) && ImageExtensions.Contains(ext);
    }

    /// <summary>默认文件选择框的筛选器。</summary>
    public static string OpenFileFilter =>
        "所有支持的文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff;*.ico;*.webp;*.jfif;*.pdf;*.doc;*.docx;*.ppt;*.pptx;*.xls;*.xlsx;*.txt;*.mp4;*.mp3;*.exe|" +
        "图片|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff;*.ico;*.webp;*.jfif|" +
        "所有文件|*.*";
}
