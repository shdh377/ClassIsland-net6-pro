using ClassIsland.Shared;
using IslandCaller.Models;
using IslandCaller.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IslandCaller.ViewModels;

public class HoverFluentViewModel : INotifyPropertyChanged
{
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

    private double _button1Width;
    public double Button1Width
    {
        get => _button1Width;
        set => SetField(ref _button1Width, value);
    }
    private double _button2Width;
    public double Button2Width
    {
        get => _button2Width;
        set => SetField(ref _button2Width, value);
    }
    private double _buttonHeight;
    public double ButtonHeight
    {
        get => _buttonHeight;
        set => SetField(ref _buttonHeight, value);
    }

    public HoverFluentViewModel()
    {
        WindowScalingFactor = Settings.Instance.Hover.ScalingFactor;
        Height = 70 * WindowScalingFactor;
        Width = 163 * WindowScalingFactor;
        PositionX = Settings.Instance.Hover.Position.X;
        PositionY = Settings.Instance.Hover.Position.Y;
        Button1Width = Width * 0.46;
        Button2Width = Width * 0.34;
        ButtonHeight = Height * 0.8;

        Settings.Instance.Hover.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Settings.Instance.Hover.ScalingFactor))
            {
                WindowScalingFactor = Settings.Instance.Hover.ScalingFactor;
                Height = 70 * WindowScalingFactor;
                Width = 163 * WindowScalingFactor;
                Button1Width = Width * 0.46;
                Button2Width = Width * 0.34;
                ButtonHeight = Height * 0.8;
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
