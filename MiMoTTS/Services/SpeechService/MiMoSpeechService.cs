using ClassIsland.Core;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared.Abstraction.Services;
using ClassIsland.Shared.Helpers;
using Microsoft.Extensions.Logging;
using MiMoTTS.Models;
using MiMoTTS.Shared;
using NAudio.Wave;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiMoTTS.Services.SpeechService;

[SpeechProviderInfo("classisland.speech.mimo-tts", "MiMo TTS")]
public class MiMoSpeechService : ISpeechService
{
    public static readonly string MiMoCacheFolderPath = Path.Combine(GlobalConstants.PluginConfigFolder, "MiMoCache");

    private readonly ILogger<MiMoSpeechService> _logger;

    private MiMoSpeechSettings _settings = new();
    private Queue<MiMoPlayInfo> PlayingQueue { get; } = new();
    private bool IsPlaying { get; set; }
    private CancellationTokenSource? RequestingCancellationTokenSource { get; set; }
    private MiMoPlayInfo? CurrentPlayInfo { get; set; }
    private WaveOutEvent? CurrentWaveOut { get; set; }

    public MiMoSpeechService(ILogger<MiMoSpeechService> logger)
    {
        _logger = logger;
        ReloadConfig();
        _logger.LogInformation("初始化了 MiMo TTS 服务。");
    }

    public void ReloadConfig()
    {
        var configPath = Path.Combine(GlobalConstants.PluginConfigFolder, "Settings.json");
        if (File.Exists(configPath))
        {
            try
            {
                // 使用 FileShare.ReadWrite，避免与设置控件保存时（FileMode.Create）互相冲突
                using var fs = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                var json = reader.ReadToEnd();
                _settings = JsonSerializer.Deserialize<MiMoSpeechSettings>(json) ?? new MiMoSpeechSettings();
            }
            catch
            {
                _settings = new MiMoSpeechSettings();
            }
        }
    }

    public void EnqueueSpeechQueue(string text)
    {
        ReloadConfig();
        var settings = _settings;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var speechText = BuildSpeechText(text, settings);
        var userPrompt = BuildUserPrompt(settings);

        _logger.LogInformation("使用模型 {Model}、音色 {Voice} 朗读文本：{Text}",
            settings.Model, settings.Voice, speechText);

        var previousCts = RequestingCancellationTokenSource;
        RequestingCancellationTokenSource = new CancellationTokenSource();
        if (previousCts is { IsCancellationRequested: false })
        {
            RequestingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                previousCts.Token,
                RequestingCancellationTokenSource.Token);
        }

        var cachePath = GetCachePath(speechText, userPrompt, settings);
        _logger.LogDebug("语音缓存路径：{CachePath}", cachePath);

        Task<bool>? downloadTask = null;
        if (!File.Exists(cachePath))
        {
            downloadTask = GenerateSpeechAsync(
                speechText,
                userPrompt,
                cachePath,
                settings,
                RequestingCancellationTokenSource.Token);
        }

        if (RequestingCancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        PlayingQueue.Enqueue(new MiMoPlayInfo(cachePath, new CancellationTokenSource(), downloadTask));
        _ = ProcessPlayerList();
    }

    public void ClearSpeechQueue()
    {
        RequestingCancellationTokenSource?.Cancel();

        try
        {
            CurrentWaveOut?.Stop();
            CurrentWaveOut?.Dispose();
            CurrentWaveOut = null;
        }
        catch
        {
            // 忽略停止播放时的异常
        }

        CurrentPlayInfo?.CancellationTokenSource.Cancel();

        while (PlayingQueue.Count > 0)
        {
            var playInfo = PlayingQueue.Dequeue();
            playInfo.CancellationTokenSource.Cancel();
        }
    }

    private static bool IsV25Model(MiMoSpeechSettings settings) =>
        string.Equals(settings.Model, MiMoSpeechSettings.ModelV25, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(settings.Model, MiMoSpeechSettings.ModelV25VoiceClone, StringComparison.OrdinalIgnoreCase);

    private static bool IsVoiceCloneModel(MiMoSpeechSettings settings) =>
        string.Equals(settings.Model, MiMoSpeechSettings.ModelV25VoiceClone, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeModel(string? model)
    {
        if (string.Equals(model, MiMoSpeechSettings.ModelV25VoiceClone, StringComparison.OrdinalIgnoreCase))
            return MiMoSpeechSettings.ModelV25VoiceClone;
        if (string.Equals(model, MiMoSpeechSettings.ModelV25, StringComparison.OrdinalIgnoreCase))
            return MiMoSpeechSettings.ModelV25;
        return MiMoSpeechSettings.ModelV2;
    }

    private string BuildSpeechText(string text, MiMoSpeechSettings settings)
    {
        var styleParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(settings.Style))
        {
            styleParts.Add(settings.Style.Trim());
        }

        if (!string.IsNullOrWhiteSpace(settings.SpeedStyle) && settings.SpeedStyle.Trim() != "默认")
        {
            styleParts.Add(settings.SpeedStyle.Trim());
        }

        if (IsV25Model(settings))
        {
            return BuildV25SpeechText(text, styleParts, settings.EnableSingingMode);
        }

        if (settings.EnableSingingMode)
        {
            styleParts.Insert(0, "唱歌");
        }

        if (styleParts.Count == 0 || text.TrimStart().StartsWith("<style>", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        return $"<style>{string.Join(" ", styleParts)}</style>{text}";
    }

    private static string BuildV25SpeechText(string text, IReadOnlyCollection<string> styleParts, bool enableSingingMode)
    {
        var trimmedText = text.TrimStart();
        if (trimmedText.StartsWith("(") || trimmedText.StartsWith("（") || trimmedText.StartsWith("["))
        {
            return text;
        }

        var prefixParts = new List<string>();
        if (enableSingingMode)
        {
            prefixParts.Add("唱歌");
        }

        prefixParts.AddRange(styleParts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part.Trim()));

        if (prefixParts.Count == 0)
        {
            return text;
        }

        return $"({string.Join(" ", prefixParts)}){text}";
    }

    private static string? BuildUserPrompt(MiMoSpeechSettings settings)
    {
        if (!settings.EnableUserPrompt || string.IsNullOrWhiteSpace(settings.UserPrompt))
        {
            return null;
        }

        return settings.UserPrompt.Trim();
    }

    private static string EnsureFormatExtension(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return "wav";
        }

        return format.Trim().TrimStart('.').ToLowerInvariant();
    }

    private string GetCachePath(string text, string? userPrompt, MiMoSpeechSettings settings)
    {
        var voiceCloneAudioHash = "";
        if (IsVoiceCloneModel(settings) && !string.IsNullOrWhiteSpace(settings.VoiceCloneAudioPath) && File.Exists(settings.VoiceCloneAudioPath))
        {
            try
            {
                var audioBytes = File.ReadAllBytes(settings.VoiceCloneAudioPath);
                var hashBytes = MD5.HashData(audioBytes);
                voiceCloneAudioHash = hashBytes.Aggregate("", (current, t) => current + t.ToString("x2"));
            }
            catch
            {
                voiceCloneAudioHash = settings.VoiceCloneAudioPath;
            }
        }

        var key = string.Join("|",
            NormalizeModel(settings.Model),
            IsVoiceCloneModel(settings) ? voiceCloneAudioHash : settings.Voice,
            settings.AudioFormat,
            settings.Style,
            settings.SpeedStyle,
            settings.EnableSingingMode,
            settings.EnableUserPrompt,
            userPrompt ?? "",
            text);
        var md5 = MD5.HashData(Encoding.UTF8.GetBytes(key));
        var md5String = md5.Aggregate("", (current, t) => current + t.ToString("x2"));
        var extension = EnsureFormatExtension(settings.AudioFormat);
        var cacheFolder = IsVoiceCloneModel(settings) ? "VoiceClone" : settings.Voice;
        var path = Path.Combine(MiMoCacheFolderPath, cacheFolder, $"{md5String}.{extension}");
        var directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory) && directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        return path;
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ClassIsland", AppBase.AppVersion));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task<bool> GenerateSpeechAsync(
        string text,
        string? userPrompt,
        string filePath,
        MiMoSpeechSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = CreateHttpClient();

            if (IsVoiceCloneModel(settings))
            {
                return await GenerateSpeechVoiceCloneAsync(httpClient, text, userPrompt, filePath, settings, cancellationToken);
            }
            else if (IsV25Model(settings))
            {
                return await GenerateSpeechV25Async(httpClient, text, userPrompt, filePath, settings, cancellationToken);
            }
            else
            {
                return await GenerateSpeechV2Async(httpClient, text, userPrompt, filePath, settings, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MiMo TTS 请求已取消。");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送 MiMo TTS 请求时发生异常。");
            return false;
        }
    }

    private async Task<bool> GenerateSpeechV2Async(
        HttpClient httpClient,
        string text,
        string? userPrompt,
        string filePath,
        MiMoSpeechSettings settings,
        CancellationToken cancellationToken)
    {
        var requestUri = $"{MiMoSpeechSettings.DefaultApiBaseUrl}/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            request.Headers.Add("api-key", settings.ApiKey);
        }

        var messages = new List<MiMoRequestMessage>();
        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            messages.Add(new MiMoRequestMessage
            {
                Role = "user",
                Content = userPrompt
            });
        }

        messages.Add(new MiMoRequestMessage
        {
            Role = "assistant",
            Content = text
        });

        var requestBody = new MiMoRequestBody
        {
            Model = NormalizeModel(settings.Model),
            Messages = messages,
            Audio = new MiMoRequestAudio
            {
                Format = EnsureFormatExtension(settings.AudioFormat),
                Voice = settings.Voice
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MiMoResponseBody>(responseJson);

        if (result?.Choices?.FirstOrDefault()?.Message?.Audio?.Data is not { } audioBase64)
        {
            _logger.LogError("MiMo TTS 响应中没有音频数据。");
            return false;
        }

        var audioBytes = Convert.FromBase64String(audioBase64);
        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);

        return true;
    }

    private async Task<bool> GenerateSpeechV25Async(
        HttpClient httpClient,
        string text,
        string? userPrompt,
        string filePath,
        MiMoSpeechSettings settings,
        CancellationToken cancellationToken)
    {
        var requestUri = $"{settings.ApiBaseUrl}/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            request.Headers.Add("api-key", settings.ApiKey);
        }

        var messages = new List<MiMoRequestMessage>();
        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            messages.Add(new MiMoRequestMessage
            {
                Role = "user",
                Content = userPrompt
            });
        }

        messages.Add(new MiMoRequestMessage
        {
            Role = "assistant",
            Content = text
        });

        var requestBody = new MiMoRequestBody
        {
            Model = NormalizeModel(settings.Model),
            Messages = messages,
            Audio = new MiMoRequestAudio
            {
                Format = EnsureFormatExtension(settings.AudioFormat),
                Voice = settings.Voice
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MiMoResponseBody>(responseJson);

        if (result?.Choices?.FirstOrDefault()?.Message?.Audio?.Data is not { } audioBase64)
        {
            _logger.LogError("MiMo TTS 响应中没有音频数据。");
            return false;
        }

        var audioBytes = Convert.FromBase64String(audioBase64);
        await File.WriteAllBytesAsync(filePath, audioBytes, cancellationToken);

        return true;
    }

    private async Task<bool> GenerateSpeechVoiceCloneAsync(
        HttpClient httpClient,
        string text,
        string? userPrompt,
        string filePath,
        MiMoSpeechSettings settings,
        CancellationToken cancellationToken)
    {
        var requestUri = $"{settings.ApiBaseUrl}/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            request.Headers.Add("api-key", settings.ApiKey);
        }

        var messages = new List<MiMoRequestMessage>();
        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            messages.Add(new MiMoRequestMessage
            {
                Role = "user",
                Content = userPrompt
            });
        }

        messages.Add(new MiMoRequestMessage
        {
            Role = "assistant",
            Content = text
        });

        var audioBytes = await File.ReadAllBytesAsync(settings.VoiceCloneAudioPath, cancellationToken);
        var audioBase64 = Convert.ToBase64String(audioBytes);

        var requestBody = new MiMoVoiceCloneRequestBody
        {
            Model = NormalizeModel(settings.Model),
            Messages = messages,
            Audio = new MiMoVoiceCloneRequestAudio
            {
                Format = EnsureFormatExtension(settings.AudioFormat),
                Voice = $"data:audio/wav;base64,{audioBase64}"
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<MiMoResponseBody>(responseJson);

        if (result?.Choices?.FirstOrDefault()?.Message?.Audio?.Data is not { } resultAudioBase64)
        {
            _logger.LogError("MiMo TTS 响应中没有音频数据。");
            return false;
        }

        var resultAudioBytes = Convert.FromBase64String(resultAudioBase64);
        await File.WriteAllBytesAsync(filePath, resultAudioBytes, cancellationToken);

        return true;
    }

    private async Task ProcessPlayerList()
    {
        if (IsPlaying)
        {
            return;
        }

        IsPlaying = true;

        while (PlayingQueue.Count > 0)
        {
            var playInfo = PlayingQueue.Dequeue();
            CurrentPlayInfo = playInfo;

            try
            {
                if (playInfo.DownloadTask != null)
                {
                    var success = await playInfo.DownloadTask;
                    if (!success)
                    {
                        continue;
                    }
                }

                if (playInfo.CancellationTokenSource.IsCancellationRequested)
                {
                    continue;
                }

                await PlayAudioFile(playInfo.CachePath, playInfo.CancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // 忽略取消异常
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "播放音频文件时发生异常。");
            }
        }

        IsPlaying = false;
        CurrentPlayInfo = null;
    }

    private Task PlayAudioFile(string filePath, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();

        try
        {
            var audioFileReader = new AudioFileReader(filePath);
            var waveOut = new WaveOutEvent();
            CurrentWaveOut = waveOut;

            waveOut.Init(audioFileReader);
            waveOut.Play();

            waveOut.PlaybackStopped += (sender, args) =>
            {
                audioFileReader.Dispose();
                waveOut.Dispose();
                CurrentWaveOut = null;
                tcs.TrySetResult();
            };

            cancellationToken.Register(() =>
            {
                try
                {
                    waveOut.Stop();
                }
                catch
                {
                    // 忽略停止时的异常
                }
            });
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }
}

internal class MiMoPlayInfo(string cachePath, CancellationTokenSource cancellationTokenSource, Task<bool>? downloadTask = null)
{
    public string CachePath { get; } = cachePath;
    public CancellationTokenSource CancellationTokenSource { get; } = cancellationTokenSource;
    public Task<bool>? DownloadTask { get; } = downloadTask;
}

internal class MiMoRequestMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

internal class MiMoRequestBody
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<MiMoRequestMessage> Messages { get; set; } = new();

    [JsonPropertyName("audio")]
    public MiMoRequestAudio Audio { get; set; } = new();
}

internal class MiMoRequestAudio
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "wav";

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = "mimo_default";
}

internal class MiMoVoiceCloneRequestBody
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<MiMoRequestMessage> Messages { get; set; } = new();

    [JsonPropertyName("audio")]
    public MiMoVoiceCloneRequestAudio Audio { get; set; } = new();
}

internal class MiMoVoiceCloneRequestAudio
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "wav";

    [JsonPropertyName("voice")]
    public string Voice { get; set; } = "";
}

internal class MiMoResponseBody
{
    [JsonPropertyName("choices")]
    public List<MiMoResponseChoice>? Choices { get; set; }
}

internal class MiMoResponseChoice
{
    [JsonPropertyName("message")]
    public MiMoResponseMessage? Message { get; set; }
}

internal class MiMoResponseMessage
{
    [JsonPropertyName("audio")]
    public MiMoResponseAudio? Audio { get; set; }
}

internal class MiMoResponseAudio
{
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}
