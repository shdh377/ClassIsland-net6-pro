using System.IO;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Shared;
using IslandCaller.Helpers;
using IslandCaller.Services;
using IslandCaller.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static IslandCaller.Services.ProfileService;

namespace IslandCaller.Views;

[SettingsPageInfo("plugins.IslandCaller", "IslandCaller 设置", SettingsPageCategory.External)]
public partial class SettingPage : SettingsPageBase
{
    private SettingPageViewModel vm;
    private HistoryService HistoryService;
    private ILogger<SettingPage> logger;

    public SettingPage()
    {
        InitializeComponent();
        vm = DataContext as SettingPageViewModel;
        HistoryService = IAppHost.GetService<HistoryService>();
        logger = IAppHost.GetService<ILogger<SettingPage>>();
        logger.LogInformation("SettingPage 初始化完成");
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        int nextId = vm.ProfileList.Any() ? vm.ProfileList.Max(s => s.ID) + 1 : 1;
        vm.ProfileList.Add(new SettingPageViewModel.StudentModel
        {
            ID = nextId,
            Name = "",
            ManualWeight = 1.0
        });
        logger.LogInformation("手动新增名单项，ID: {Id}", nextId);
    }

    private async void ImportButton_OnClick(object sender, RoutedEventArgs e)
    {
        List<Person> newlist = new List<Person>();
        logger.LogInformation("开始导入名单流程");

        await CommonTaskDialogs.ShowDialog("导入提示", "导入的名单仅支持下列格式: \n\n" +
            "文本名单 (*.txt): 名单仅包含姓名，使用空格，逗号，或换行分隔\n\n" +
            "SecRandom 名单 (*.json)\n\n" +
            "CSV 名单 (*.csv): 名单包含姓名，不能含有标题");

        var dialog = new OpenFileDialog
        {
            Title = "选择要导入的名单",
            Filter = "文本名单 (*.txt)|*.txt|SecRandom 名单 (*.json)|*.json|CSV 名单 (*.csv)|*.csv",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
        {
            logger.LogInformation("用户取消了名单导入");
            return;
        }

        string filePath = dialog.FileName;
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        logger.LogInformation("已选择导入文件：{FileName}，扩展名：{Extension}", filePath, extension);

        try
        {
            switch (extension)
            {
                case ".txt":
                    newlist = new TextFileParseHelper().ParseTextFile(filePath);
                    break;
                case ".json":
                    newlist = new SecRandomParseHelper().ParseSecRandomProfile(filePath);
                    break;
                case ".csv":
                    var csvDialog = new CsvImport();
                    csvDialog.Owner = Window.GetWindow(this);
                    if (csvDialog.ShowDialog() == true)
                    {
                        newlist = new CsvParseHelper().ParseCsvFile(filePath, csvDialog.NameRow);
                    }
                    break;
                default:
                    logger.LogError("导入名单失败：不支持的文件类型 {Extension}", extension);
                    await CommonTaskDialogs.ShowDialog("导入失败", $"不支持的文件类型：{extension}");
                    return;
            }

            var orderedProfile = newlist
                .OrderBy(m => m.Id)
                .Select(m => new SettingPageViewModel.StudentModel
                {
                    ID = m.Id,
                    Name = m.Name,
                    ManualWeight = m.ManualWeight
                });

            vm.ProfileList = new ObservableCollection<SettingPageViewModel.StudentModel>(orderedProfile);
            logger.LogInformation("名单导入成功，共导入 {Count} 人", vm.ProfileList.Count);
            await CommonTaskDialogs.ShowDialog("导入完成", $"成功导入 {vm.ProfileList.Count} 条名单。");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "导入名单过程中发生异常，文件：{FileName}", filePath);
            await CommonTaskDialogs.ShowDialog("导入失败", "导入名单时发生错误，请检查文件格式后重试。");
        }
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        logger.LogInformation("清空点名历史记录");
        HistoryService.ClearThisLessonHistory();
        HistoryService.ClearLongTermHistory();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        logger.LogInformation("手动保存档案设置");
        vm.SaveProfile();
    }
}
