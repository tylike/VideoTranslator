using System.Collections.Generic;

namespace VideoTranslator.Config;

[Obsolete("准备删除",true)]
public class AppSettingsX
{
    #region 构造函数

    public AppSettingsX()
    {
        LoadFromConfiguration();
    }

    #endregion

    #region 从配置加载

    private void LoadFromConfiguration()
    {
        try
        {
            var config = ConfigurationService.Configuration;
            var vtConfig = config.VideoTranslator;

            FfmpegPath = vtConfig.Paths.FfmpegPath;
            YtDlpPath = vtConfig.Paths.YtDlpPath;
            WhisperPath = vtConfig.Paths.WhisperPath;
            WhisperModelPath = vtConfig.Paths.WhisperModelPath;
            WhisperSmallModelPath = vtConfig.Paths.WhisperSmallModelPath;
            BaseDirectory = vtConfig.Paths.BaseDirectory;
            PythonPath = vtConfig.Paths.PythonPath;
            AudioSeparationScriptPath = vtConfig.Paths.AudioSeparationScriptPath;
            DemucsOutputBasePath = vtConfig.Paths.DemucsOutputBasePath;
            VoskModelPath = vtConfig.Paths.VoskModelPath;
            VoskSpeakerModelPath = vtConfig.Paths.VoskSpeakerModelPath;
            PurfviewFasterWhisperPath = vtConfig.Paths.PurfviewFasterWhisperPath;
            PurfviewFasterWhisperModelPath = vtConfig.Paths.PurfviewFasterWhisperModelPath;
            SherpaSegmentationModelPath = vtConfig.Paths.SherpaSegmentationModelPath;
            SherpaEmbeddingModelPath = vtConfig.Paths.SherpaEmbeddingModelPath;
            SherpaEmbeddingModelPathEn = vtConfig.Paths.SherpaEmbeddingModelPathEn;

            LMStudioApiUrl = vtConfig.LLM.ApiUrl;
            LMStudioApiKey = vtConfig.LLM.ApiKey;
            LMStudioModelName = vtConfig.LLM.ModelName;

            TTSServers = vtConfig.TTS.Servers;
            KeepFiles = vtConfig.General.KeepFiles;
        }
        catch (FileNotFoundException ex)
        {
            throw new InvalidOperationException($"配置文件未找到: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"加载配置失败: {ex.Message}", ex);
        }
    }

    #endregion

    #region 路径配置

    public string FfmpegPath { get; set; } = "ffmpeg";
    public string YtDlpPath { get; set; } = "yt-dlp";
    public string WhisperPath { get; set; } = "whisper-cli.exe";
    public string WhisperModelPath { get; set; } = @"D:\VideoTranslator\whisper.cpp\ggml-tiny.en.bin";
    public string WhisperSmallModelPath { get; set; }= @"D:\VideoTranslator\whisper.cpp\ggml-tiny.en.bin";
    public string BaseDirectory { get; set; } = @"d:\index-tts\Audio.En2Cn\projects";
    public string PythonPath { get; set; } = "C:\\Users\\Administrator\\AppData\\Local\\Programs\\Python\\Python310\\python.exe";
    public string AudioSeparationScriptPath { get; set; } = @"d:\VideoTranslator\VT\VT.Module\PythonScripts\demucs_run.py";
    public string DemucsOutputBasePath { get; set; } = @"d:\VideoTranslator\videoProjects";
    public string SpleeterExePath { get; set; } = @"d:\VideoTranslator\Spleeter\Spleeter.exe";
    public string SpleeterModelPath { get; set; } = "2stems";

    #endregion

    #region LLM Configuration

    public string LMStudioApiUrl { get; set; } = "http://127.0.0.1:1235/v1/chat/completions";
    public string LMStudioApiKey { get; set; } = "dummy-key";
    public string LMStudioModelName { get; set; } = "glm4.7-flash";

    #endregion

    #region TTS Configuration

    public List<string> TTSServers { get; set; } = new List<string>();
    public bool KeepFiles { get; set; } = true;

    #endregion

    #region VAD & Whisper Server Configuration

    public string WhisperServerUrl { get; set; } = "http://127.0.0.1:8080";
    public string VadModelPath { get; set; } = @"d:\VideoTranslator\whisper.cpp\ggml-silero-v6.2.0-vad.bin";
    public string VadExecutablePath { get; set; } = @"d:\VideoTranslator\whisper.cpp\whisper-vad-speech-segments.exe";

    #endregion

    #region Vosk Configuration

    public string VoskModelPath { get; set; } = @"C:\Users\Administrator\AppData\Roaming\Subtitle Edit\Vosk\vosk-model-en-us-0.22";
    public string VoskSpeakerModelPath { get; set; } = @"D:\VideoTranslator\第三方项目\stt\vosk-speaker-model\vosk-model-spk-0.4";

    #endregion

    #region PurfviewFasterWhisper Configuration

    public string PurfviewFasterWhisperPath { get; set; } = @"C:\Users\Administrator\AppData\Roaming\Subtitle Edit\Whisper\Purfview-Whisper-Faster\faster-whisper-xxl.exe";
    public string PurfviewFasterWhisperModelPath { get; set; } = @"large-v3";

    #endregion

    #region Speaker Diarization Configuration

    public string SherpaSegmentationModelPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-pyannote-segmentation-3-0\model.onnx";

    public string SherpaEmbeddingModelPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx.models\3dspeaker_speech_eres2net_base_sv_zh-cn_3dspeaker_16k.onnx";

    public string SherpaEmbeddingModelPathEn { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx.models\wespeaker_en_voxceleb_resnet34_LM.onnx";

    #endregion
}
