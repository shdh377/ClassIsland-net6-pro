using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Services;
using ClassIsland.Shared.Abstraction.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClassIsland.Core.Extensions.Registry;

/// <summary>
/// 用于注册语音提供方的 IServiceCollection 扩展。
/// </summary>
public static class SpeechProviderRegistryExtensions
{
    private static SpeechProviderRegistry? _sharedRegistry;

    /// <summary>
    /// 获取或创建共享的注册表实例
    /// </summary>
    private static SpeechProviderRegistry GetOrCreateRegistry()
    {
        if (_sharedRegistry == null)
        {
            _sharedRegistry = new SpeechProviderRegistry();
        }
        return _sharedRegistry;
    }

    /// <summary>
    /// 注册一个语音提供方
    /// </summary>
    /// <typeparam name="TSpeechService">语音服务类型，实现 ISpeechService</typeparam>
    /// <typeparam name="TSettingsControl">设置控件类型，继承 SpeechProviderControlBase</typeparam>
    /// <param name="name">显示名称</param>
    /// <param name="index">索引号（可选）</param>
    public static IServiceCollection AddSpeechProvider<TSpeechService, TSettingsControl>(this IServiceCollection services, string? name = null, int? index = null)
        where TSpeechService : class, ISpeechService
        where TSettingsControl : SpeechProviderControlBase
    {
        // 注册语音服务为单例
        services.AddSingleton<ISpeechService, TSpeechService>();

        // 注册到共享注册表（含设置控件类型）
        var registry = GetOrCreateRegistry();
        var providerName = name ?? typeof(TSpeechService).Name;
        registry.RegisterProvider<TSpeechService, TSettingsControl>(providerName, index);

        // 确保注册表在 DI 中可用
        services.AddSingleton(registry);

        return services;
    }

    /// <summary>
    /// 注册一个语音提供方（无设置控件）
    /// </summary>
    public static IServiceCollection AddSpeechProvider<TSpeechService>(this IServiceCollection services, string? name = null, int? index = null)
        where TSpeechService : class, ISpeechService
    {
        services.AddSingleton<ISpeechService, TSpeechService>();

        // 注册到共享注册表
        var registry = GetOrCreateRegistry();
        var providerName = name ?? typeof(TSpeechService).Name;
        registry.RegisterProvider<TSpeechService>(providerName, index);

        // 确保注册表在 DI 中可用
        services.AddSingleton(registry);

        return services;
    }
}
