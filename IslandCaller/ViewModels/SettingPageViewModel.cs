using ClassIsland.Shared;
using CommunityToolkit.Mvvm.Input;
using IslandCaller.Models;
using IslandCaller.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using static IslandCaller.Services.ProfileService;

namespace IslandCaller.ViewModels;

public class SettingPageViewModel : INotifyPropertyChanged
{
    // 基本设置
    private bool _isBreakDisable;
    public bool IsBreakDisable
    {
        get => _isBreakDisable;
        set => SetField(ref _isBreakDisable, value);
    }

    private bool _interruptable;
    public bool Interruptable
    {
        get => _interruptable;
        set => SetField(ref _interruptable, value);
    }

    //悬浮窗设置
    private bool _isHoverEnable;
    public bool IsHoverEnable
    {
        get => _isHoverEnable;
        set => SetField(ref _isHoverEnable, value);
    }
    private double _hoverScalingFactor;
    public double HoverScalingFactor
    {
        get => _hoverScalingFactor;
        set => SetField(ref _hoverScalingFactor, value);
    }

    // 档案设置
    private Guid _currentProfile = Settings.Instance.Profile.DefaultProfile;
    public Guid CurrentProfile { get => _currentProfile; }

    public class StudentModel : INotifyPropertyChanged
    {
        private int _id;
        public int ID
        {
            get => _id;
            set => SetField(ref _id, value);
        }
        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }
        private double _manualWeight;
        public double ManualWeight
        {
            get => _manualWeight;
            set => SetField(ref _manualWeight, value);
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

    private readonly Dictionary<StudentModel, PropertyChangedEventHandler> _handlers = new();
    private readonly ProfileService _profileService;
    private readonly HistoryService _historyService;
    private readonly CoreService _coreService;
    private readonly DispatcherTimer _persistTimer;
    private bool _isInitializing = true;
    private ObservableCollection<StudentModel> _profileList = new();
    public ObservableCollection<StudentModel> ProfileList
    {
        get => _profileList;
        set
        {
            if (SetField(ref _profileList, value))
            {
                AttachHandlersToCurrentList();
            }
        }
    }

    public ICommand RowCommand => new RelayCommand<StudentModel>(row =>
    {
        var firstColumnValue = row?.ID;
        var item = ProfileList.FirstOrDefault(p => p.ID == firstColumnValue);
        if (item != null)
        {
            ProfileList.Remove(item);
        }
    });

    public SettingPageViewModel()
    {
        _profileService = IAppHost.GetService<ProfileService>();
        _historyService = IAppHost.GetService<HistoryService>();
        _coreService = IAppHost.GetService<CoreService>();

        _persistTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _persistTimer.Tick += (_, _) =>
        {
            _persistTimer.Stop();
            SaveProfile();
        };

        IsBreakDisable = Settings.Instance.General.BreakDisable;
        Interruptable = Settings.Instance.General.Interruptable;
        IsHoverEnable = Settings.Instance.Hover.IsEnable;
        HoverScalingFactor = Settings.Instance.Hover.ScalingFactor;
        ProfileList = new ObservableCollection<StudentModel>(
            _profileService.GetMembers(CurrentProfile)
                .OrderBy(m => m.Id)
                .Select(m => new StudentModel
                {
                    ID = m.Id,
                    Name = m.Name,
                    ManualWeight = m.ManualWeight
                }));

        this.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(IsBreakDisable))
            {
                Settings.Instance.General.BreakDisable = IsBreakDisable;
            }
            else if (args.PropertyName == nameof(Interruptable))
            {
                Settings.Instance.General.Interruptable = Interruptable;
            }
            else if (args.PropertyName == nameof(IsHoverEnable))
            {
                Settings.Instance.Hover.IsEnable = IsHoverEnable;
            }
            else if (args.PropertyName == nameof(HoverScalingFactor))
            {
                Settings.Instance.Hover.ScalingFactor = HoverScalingFactor;
            }
        };
        _isInitializing = false;
    }

    private void AttachHandlersToCurrentList()
    {
        _profileList.CollectionChanged += ProfileList_OnCollectionChanged;
        foreach (var student in _profileList)
        {
            AttachStudentHandler(student);
        }
        if (!_isInitializing)
        {
            SaveProfile();
        }
    }

    private void ProfileList_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (StudentModel student in e.NewItems)
            {
                AttachStudentHandler(student);
            }
        }
        if (e.OldItems != null)
        {
            foreach (StudentModel student in e.OldItems)
            {
                DetachStudentHandler(student);
            }
        }
        SaveProfile();
    }

    private void AttachStudentHandler(StudentModel student)
    {
        if (_handlers.ContainsKey(student)) return;
        PropertyChangedEventHandler handler = (_, _) => ApplyChanges();
        _handlers[student] = handler;
        student.PropertyChanged += handler;
    }

    private void DetachStudentHandler(StudentModel student)
    {
        if (_handlers.TryGetValue(student, out var handler))
        {
            student.PropertyChanged -= handler;
            _handlers.Remove(student);
        }
    }

    /// <summary>
    /// 编辑单元格时调用：立即在内存中生效（不写磁盘、不翻转 Status 标志），
    /// 并通过防抖在停止编辑约 800ms 后自动落盘。
    /// </summary>
    private void ApplyChanges()
    {
        List<Person> list = ProfileList
            .Select(s => new Person
            {
                Id = s.ID,
                Name = s.Name,
                Gender = 0,
                ManualWeight = s.ManualWeight
            }).ToList();
        _profileService.Members = list;
        _coreService.ApplyMemberWeights();
        SchedulePersist();
    }

    private void SchedulePersist()
    {
        _persistTimer.Stop();
        _persistTimer.Start();
    }

    /// <summary>
    /// 完整保存：写入名单文件、刷新历史记录并重新初始化核心。
    /// 由保存按钮、防抖、增删、导入时调用。
    /// </summary>
    public void SaveProfile()
    {
        List<Person> list = ProfileList
            .Select(s => new Person
            {
                Id = s.ID,
                Name = s.Name,
                Gender = 0,
                ManualWeight = s.ManualWeight
            }).ToList();
        _profileService.Members = list;
        _profileService.SaveProfile(CurrentProfile, list);
        _historyService.Load(CurrentProfile);
        _coreService.InitializeCore();
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
