using CommunityToolkit.Mvvm.ComponentModel;

namespace MiMoTTS.Models;

public class MiMoSpeechSettings : ObservableRecipient
{
    public const string DefaultApiBaseUrl = "https://api.xiaomimimo.com/v1";
    public const string TokenPlanApiBaseUrl = "https://token-plan-cn.xiaomimimo.com/v1";
    public const string ModelV2 = "mimo-v2-tts";
    public const string ModelV25 = "mimo-v2.5-tts";
    public const string ModelV25VoiceClone = "mimo-v2.5-tts-voiceclone";

    private string _apiBaseUrl = DefaultApiBaseUrl;
    private string _apiKey = "";
    private string _model = ModelV2;
    private string _voice = "mimo_default";
    private string _audioFormat = "wav";
    private string _style = "";
    private string _speedStyle = "默认";
    private bool _enableUserPrompt = true;
    private string _userPrompt = "请自然、清晰地朗读内容。";
    private bool _enableSingingMode;
    private string _voiceCloneAudioPath = "";

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set
        {
            if (value == _apiBaseUrl) return;
            _apiBaseUrl = value;
            OnPropertyChanged();
        }
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (value == _apiKey) return;
            _apiKey = value;
            OnPropertyChanged();
        }
    }

    public string Model
    {
        get => _model;
        set
        {
            if (value == _model) return;
            _model = value;
            OnPropertyChanged();
        }
    }

    public string Voice
    {
        get => _voice;
        set
        {
            if (value == _voice) return;
            _voice = value;
            OnPropertyChanged();
        }
    }

    public string AudioFormat
    {
        get => _audioFormat;
        set
        {
            if (value == _audioFormat) return;
            _audioFormat = value;
            OnPropertyChanged();
        }
    }

    public string Style
    {
        get => _style;
        set
        {
            if (value == _style) return;
            _style = value;
            OnPropertyChanged();
        }
    }

    public string SpeedStyle
    {
        get => _speedStyle;
        set
        {
            if (value == _speedStyle) return;
            _speedStyle = value;
            OnPropertyChanged();
        }
    }

    public bool EnableUserPrompt
    {
        get => _enableUserPrompt;
        set
        {
            if (value == _enableUserPrompt) return;
            _enableUserPrompt = value;
            OnPropertyChanged();
        }
    }

    public string UserPrompt
    {
        get => _userPrompt;
        set
        {
            if (value == _userPrompt) return;
            _userPrompt = value;
            OnPropertyChanged();
        }
    }

    public bool EnableSingingMode
    {
        get => _enableSingingMode;
        set
        {
            if (value == _enableSingingMode) return;
            _enableSingingMode = value;
            OnPropertyChanged();
        }
    }

    public string VoiceCloneAudioPath
    {
        get => _voiceCloneAudioPath;
        set
        {
            if (value == _voiceCloneAudioPath) return;
            _voiceCloneAudioPath = value;
            OnPropertyChanged();
        }
    }
}
