using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoTranslator.Config;

public class ConfigurationService
{
    private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private static ConfigurationRoot? _configuration;
    private static readonly object _lock = new object();

    private ConfigurationService()
    {
    }

    public static ConfigurationRoot Configuration
    {
        get
        {
            lock (_lock)
            {
                if (_configuration == null)
                {
                    LoadConfiguration();
                }
                return _configuration;
            }
        }
    }

    public static void LoadConfiguration()
    {
        lock (_lock)
        {
            if (!File.Exists(ConfigFilePath))
            {
                throw new FileNotFoundException($"配置文件不存在: {ConfigFilePath}");
            }

            try
            {
                var json = File.ReadAllText(ConfigFilePath);
                _configuration = JsonSerializer.Deserialize<ConfigurationRoot>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                });

                if (_configuration == null)
                {
                    throw new InvalidOperationException("配置文件解析失败");
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"配置文件格式错误: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"读取配置文件时出错: {ex.Message}", ex);
            }
        }
    }

    public static void SaveConfiguration()
    {
        lock (_lock)
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("配置未加载，无法保存");
            }

            try
            {
                var json = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存配置文件时出错: {ex.Message}", ex);
            }
        }
    }

    public static void ReloadConfiguration()
    {
        lock (_lock)
        {
            LoadConfiguration();
        }
    }

    public static string GetConfigFilePath()
    {
        return ConfigFilePath;
    }
}

#region 配置模型

public class ConfigurationRoot
{
    [JsonPropertyName("VideoTranslator")]
    public VideoTranslatorConfig VideoTranslator { get; set; } = new();

    [JsonPropertyName("Database")]
    public DatabaseConfig Database { get; set; } = new();
}

public class VideoTranslatorConfig
{
    [JsonPropertyName("Paths")]
    public PathConfig Paths { get; set; } = new();

    [JsonPropertyName("LLM")]
    public LLMConfig LLM { get; set; } = new();

    [JsonPropertyName("TTS")]
    public TTSConfig TTS { get; set; } = new();

    [JsonPropertyName("General")]
    public GeneralConfig General { get; set; } = new();
}

public class PathConfig
{
    [JsonPropertyName("FfmpegPath")]
    public string FfmpegPath { get; set; } = "ffmpeg";

    [JsonPropertyName("YtDlpPath")]
    public string YtDlpPath { get; set; } = "yt-dlp";

    [JsonPropertyName("WhisperPath")]
    public string WhisperPath { get; set; } = "whisper-cli.exe";

    [JsonPropertyName("WhisperModelPath")]
    public string WhisperModelPath { get; set; } = @"D:\VideoTranslator\whisper.cpp\ggml-tiny.en.bin";

    [JsonPropertyName("BaseDirectory")]
    public string BaseDirectory { get; set; } = @"d:\index-tts\Audio.En2Cn\projects";

    [JsonPropertyName("PythonPath")]
    public string PythonPath { get; set; } = "python";

    [JsonPropertyName("AudioSeparationScriptPath")]
    public string AudioSeparationScriptPath { get; set; } = @"d:\VideoTranslator\VT\VT.Module\PythonScripts\demucs_run.py";

    [JsonPropertyName("DemucsOutputBasePath")]
    public string DemucsOutputBasePath { get; set; } = @"d:\VideoTranslator\videoProjects";

    [JsonPropertyName("VoskModelPath")]
    public string VoskModelPath { get; set; } = @"C:\Users\Administrator\AppData\Roaming\Subtitle Edit\Vosk\vosk-model-en-us-0.22";

    [JsonPropertyName("VoskSpeakerModelPath")]
    public string VoskSpeakerModelPath { get; set; } = @"D:\VideoTranslator\第三方项目\stt\vosk-speaker-model\vosk-model-spk-0.4";

    [JsonPropertyName("PurfviewFasterWhisperPath")]
    public string PurfviewFasterWhisperPath { get; set; } = @"C:\Users\Administrator\AppData\Roaming\Subtitle Edit\Whisper\Purfview-Whisper-Faster\faster-whisper-xxl.exe";

    [JsonPropertyName("PurfviewFasterWhisperModelPath")]
    public string PurfviewFasterWhisperModelPath { get; set; } = "large-v3";

    [JsonPropertyName("SherpaSegmentationModelPath")]
    public string SherpaSegmentationModelPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-pyannote-segmentation-3-0\model.onnx";

    [JsonPropertyName("SherpaEmbeddingModelPath")]
    public string SherpaEmbeddingModelPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx.models\3dspeaker_speech_eres2net_base_sv_zh-cn_3dspeaker_16k.onnx";

    [JsonPropertyName("SherpaEmbeddingModelPathEn")]
    public string SherpaEmbeddingModelPathEn { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx.models\wespeaker_en_voxceleb_resnet34_LM.onnx";

    [JsonPropertyName("NodePath")]
    public string NodePath { get; set; } = @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Microsoft\VisualStudio\NodeJs\node.exe";
        
    public string WhisperSmallModelPath { get;  set; }
    public string SpleeterExePath { get; set; } = @"d:\VideoTranslator\Spleeter\Spleeter.exe";
    public string SpleeterModelPath { get; set; } = "2stems";
    public string WhisperServerUrl { get; set; }
}

public class LLMConfig
{
    [JsonPropertyName("ApiUrl")]
    public string ApiUrl { get; set; } = "http://127.0.0.1:1235/v1/chat/completions";

    [JsonPropertyName("ApiKey")]
    public string ApiKey { get; set; } = "dummy-key";

    [JsonPropertyName("ModelName")]
    public string ModelName { get; set; } = "glm4.7-flash";
}

public class TTSConfig
{
    [JsonPropertyName("Servers")]
    public List<string> Servers { get; set; } = new();
}

public class GeneralConfig
{
    [JsonPropertyName("KeepFiles")]
    public bool KeepFiles { get; set; } = true;
}

public class DatabaseConfig
{
    [JsonPropertyName("ConnectionString")]
    public string ConnectionString { get; set; } = string.Empty;
}

#endregion
