using ClassIsland.Shared.Abstraction.Services;

namespace ClassIsland.Core.Services;

/// <summary>
/// 语音提供方注册表，用于管理所有注册的语音提供方
/// </summary>
public class SpeechProviderRegistry
{
    private readonly Dictionary<int, Type> _providers = new();
    private readonly Dictionary<int, string> _providerNames = new();
    private readonly Dictionary<int, Type> _settingsControlTypes = new();
    private int _nextIndex = 3; // 0, 1, 2 are reserved for built-in providers

    /// <summary>
    /// 注册一个语音提供方
    /// </summary>
    /// <typeparam name="TSpeechService">语音服务类型</typeparam>
    /// <param name="name">显示名称</param>
    /// <param name="index">索引号（可选，如果不指定则自动分配）</param>
    /// <returns>分配的索引号</returns>
    public int RegisterProvider<TSpeechService>(string name, int? index = null)
        where TSpeechService : class, ISpeechService
    {
        var providerIndex = index ?? _nextIndex++;
        _providers[providerIndex] = typeof(TSpeechService);
        _providerNames[providerIndex] = name;
        return providerIndex;
    }

    /// <summary>
    /// 注册一个带设置控件的语音提供方
    /// </summary>
    public int RegisterProvider<TSpeechService, TSettingsControl>(string name, int? index = null)
        where TSpeechService : class, ISpeechService
        where TSettingsControl : class
    {
        var providerIndex = RegisterProvider<TSpeechService>(name, index);
        _settingsControlTypes[providerIndex] = typeof(TSettingsControl);
        return providerIndex;
    }

    /// <summary>
    /// 获取指定索引的设置控件类型
    /// </summary>
    public Type? GetSettingsControlType(int index)
    {
        return _settingsControlTypes.TryGetValue(index, out var type) ? type : null;
    }

    /// <summary>
    /// 获取指定索引的语音提供方类型
    /// </summary>
    public Type? GetProviderType(int index)
    {
        return _providers.TryGetValue(index, out var type) ? type : null;
    }

    /// <summary>
    /// 获取指定索引的语音提供方名称
    /// </summary>
    public string? GetProviderName(int index)
    {
        return _providerNames.TryGetValue(index, out var name) ? name : null;
    }

    /// <summary>
    /// 获取所有注册的语音提供方
    /// </summary>
    public IReadOnlyDictionary<int, string> GetAllProviders()
    {
        return _providerNames;
    }

    /// <summary>
    /// 检查指定索引是否有注册的语音提供方
    /// </summary>
    public bool HasProvider(int index)
    {
        return _providers.ContainsKey(index);
    }
}
