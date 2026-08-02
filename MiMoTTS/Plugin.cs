using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiMoTTS.Controls.SpeechProviderSettingsControls;
using MiMoTTS.Services.SpeechService;
using MiMoTTS.Shared;

namespace MiMoTTS;

[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 注册 MiMoTTS 语音提供方，索引为 3
        services.AddSpeechProvider<MiMoSpeechService, MiMoSpeechServiceSettingsControl>("MiMo TTS", 3);
        GlobalConstants.PluginConfigFolder = PluginConfigFolder;
    }
}
