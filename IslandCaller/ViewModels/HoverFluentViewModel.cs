using ClassIsland.Shared;
using IslandCaller.Models;
using IslandCaller.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IslandCaller.ViewModels;

public class HoverFluentViewModel : INotifyPropertyChanged
{
    /// <summary>Call 按钮基准宽：105（宽:高 = 1.5:1）</summary>
    private const double BaseButton1Width = 105;
    /// <summary>... 按钮基准宽：70（1:1 正方形，与按钮高相等）</summary>
    private const double BaseButton2Width = 70;
    /// <summary>按钮基准高：70</summary>
    private const double BaseButtonHeight = 70;
    /// <summary>两按钮之间基准间隔：10</summary>
    private const double BaseGap = 10;
    /// <summary>文字基准字号：22（随缩放系数等比变化，视觉美观）</summary>
    private const double BaseFontSize = 22;

    private double _windowScalingFactor;
    public double WindowScalingFactor
    {
        get => _windowScalingFactor;
        set => SetField(ref _windowScalingFactor, value);
    }

    private bool _isenabled;
    public bool IsEnabled
    {
        get => _isenabled;
        set
        {
            SetField(ref _isenabled, value);
            OnPropertyChanged(nameof(Button1Content));
            OnPropertyChanged(nameof(Button2Content));
        }
    }

    public string Button1Content => IsEnabled ? "Call" : "...";
    public string Button2Content => IsEnabled ? "..." : "...";

    private double _height;
    public double Height
    {
        get => _height;
        set => SetField(ref _height, value);
    }

    private double _width;
    public double Width
    {
        get => _width;
        set => SetField(ref _width, value);
    }

    private double _gridWidth;
    /// <summary>内容网格总宽 = Call宽 + 间隔 + ...宽</summary>
    public double GridWidth
    {
        get => _gridWidth;
        set => SetField(ref _gridWidth, value);
    }

    private double _gridHeight;
    public double GridHeight
    {
        get => _gridHeight;
        set => SetField(ref _gridHeight, value);
    }

    private double _button1Width;
    /// <summary>Call 按钮宽度（= 164.5 × 缩放系数，宽:高 = 2.35:1）</summary>
    public double Button1Width
    {
        get => _button1Width;
        set => SetField(ref _button1Width, value);
    }

    private double _button2Width;
    /// <summary>... 按钮宽度（= 140 × 缩放系数，宽:高 = 2:1）</summary>
    public double Button2Width
    {
        get => _button2Width;
        set => SetField(ref _button2Width, value);
    }

    private double _buttonHeight;
    /// <summary>按钮高度（= 70 × 缩放系数）</summary>
    public double ButtonHeight
    {
        get => _buttonHeight;
        set => SetField(ref _buttonHeight, value);
    }

    private double _gap;
    /// <summary>两按钮间隔（= 10 × 缩放系数）</summary>
    public double Gap
    {
        get => _gap;
        set => SetField(ref _gap, value);
    }

    private double _fontSize;
    /// <summary>按钮文字字号：= 18 × 缩放系数，随悬浮窗大小等比变化（美观）</summary>
    public double FontSize
    {
        get => _fontSize;
        set => SetField(ref _fontSize, value);
    }

    private double _positionX;
    public double PositionX
    {
        get => _positionX;
        set
        {
            SetField(ref _positionX, value);
            Settings.Instance.Hover.Position.X = value;
        }
    }

    private double _positionY;
    public double PositionY
    {
        get => _positionY;
        set
        {
            SetField(ref _positionY, value);
            Settings.Instance.Hover.Position.Y = value;
        }
    }


    public HoverFluentViewModel()
    {
        WindowScalingFactor = Settings.Instance.Hover.ScalingFactor;
        ApplyScaling();
        PositionX = Settings.Instance.Hover.Position.X;
        PositionY = Settings.Instance.Hover.Position.Y;

        Settings.Instance.Hover.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Settings.Instance.Hover.ScalingFactor))
            {
                WindowScalingFactor = Settings.Instance.Hover.ScalingFactor;
                ApplyScaling();
            }
        };
        var status = IAppHost.GetService<Status>();
        IsEnabled = status.IsPluginReady;
        status.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(status.IsPluginReady))
            {
                IsEnabled = status.IsPluginReady;
            }
        };
    }

    /// <summary>按缩放系数计算窗口/网格/按钮/间隔/字号尺寸</summary>
    private void ApplyScaling()
    {
        var s = WindowScalingFactor;
        Button1Width = BaseButton1Width * s;
        Button2Width = BaseButton2Width * s;
        ButtonHeight = BaseButtonHeight * s;
        Gap = BaseGap * s;
        FontSize = BaseFontSize * s;
        GridWidth = Button1Width + Gap + Button2Width;
        GridHeight = ButtonHeight;
        Width = GridWidth;
        Height = GridHeight;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
