using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IslandCaller.Services;

public class Status : INotifyPropertyChanged
{
    private bool _profileServiceInitialized;
    public bool ProfileServiceInitialized
    {
        get => _profileServiceInitialized;
        set => SetField(ref _profileServiceInitialized, value);
    }

    private bool _historyServiceInitialized;
    public bool HistoryServiceInitialized
    {
        get => _historyServiceInitialized;
        set => SetField(ref _historyServiceInitialized, value);
    }

    private bool _coreServiceInitialized;
    public bool CoreServiceInitialized
    {
        get => _coreServiceInitialized;
        set => SetField(ref _coreServiceInitialized, value);
    }

    private bool _islandCallerServiceInitialized;
    public bool IslandCallerServiceInitialized
    {
        get => _islandCallerServiceInitialized;
        set => SetField(ref _islandCallerServiceInitialized, value);
    }

    private bool _isTimeStatusAvailable;
    public bool IsTimeStatusAvailable
    {
        get => _isTimeStatusAvailable;
        set => SetField(ref _isTimeStatusAvailable, value);
    }

    private bool _occupationDisable;
    public bool OccupationDisable
    {
        get => _occupationDisable;
        set => SetField(ref _occupationDisable, value);
    }

    private bool _interruptionEnable;
    public bool InterruptionEnable
    {
        get => _interruptionEnable;
        set => SetField(ref _interruptionEnable, value);
    }

    private bool _isPluginReady;
    public bool IsPluginReady
    {
        get => _isPluginReady;
        private set => SetField(ref _isPluginReady, value);
    }

    public Status()
    {
        ProfileServiceInitialized = false;
        HistoryServiceInitialized = false;
        CoreServiceInitialized = false;
        IslandCallerServiceInitialized = false;
        InterruptionEnable = false;
        IsTimeStatusAvailable = false;
        OccupationDisable = true;

        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProfileServiceInitialized) ||
                args.PropertyName == nameof(HistoryServiceInitialized) ||
                args.PropertyName == nameof(CoreServiceInitialized) ||
                args.PropertyName == nameof(IslandCallerServiceInitialized) ||
                args.PropertyName == nameof(InterruptionEnable) ||
                args.PropertyName == nameof(IsTimeStatusAvailable) ||
                args.PropertyName == nameof(OccupationDisable))
            {
                IsPluginReady = ProfileServiceInitialized &&
                                HistoryServiceInitialized &&
                                CoreServiceInitialized &&
                                IslandCallerServiceInitialized &&
                                IsTimeStatusAvailable &&
                                (OccupationDisable || InterruptionEnable);
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
