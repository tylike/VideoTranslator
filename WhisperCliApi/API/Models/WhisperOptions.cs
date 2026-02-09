using WhisperCliApi.Enums;

namespace WhisperCliApi.Models;

public class WhisperOptions
{
    #region 输入输出配置

    public string? AudioFilePath { get; set; }

    public string? OutputFilePath { get; set; }

    public List<OutputFormat> OutputFormats { get; set; } = new();

    public bool PrintProgress { get; set; } = true;

    public bool NoPrints { get; set; }

    #endregion

    #region 模型配置

    public string? ModelPath { get; set; }

    public Language Language { get; set; } = Language.Auto;

    public bool DetectLanguage { get; set; }

    public string? Prompt { get; set; }

    public bool CarryInitialPrompt { get; set; }

    #endregion

    #region 音频处理配置

    public int? AudioContext { get; set; }

    public int? Offset { get; set; }

    public int? Duration { get; set; }

    public int? MaxContext { get; set; }

    public int? MaxLength { get; set; }

    public bool SplitOnWord { get; set; }

    #endregion

    #region 解码配置

    public int? BestOf { get; set; }

    public int? BeamSize { get; set; }

    public double? Temperature { get; set; }

    public double? TemperatureInc { get; set; }

    public double? WordThreshold { get; set; }

    public double? EntropyThreshold { get; set; }

    public double? LogProbThreshold { get; set; }

    public double? NoSpeechThreshold { get; set; }

    public bool NoFallback { get; set; }

    public bool Translate { get; set; }

    #endregion

    #region 高级配置

    public bool DebugMode { get; set; }

    public bool Diarize { get; set; }

    public bool TinyDiarize { get; set; }

    public bool PrintSpecial { get; set; }

    public bool PrintColors { get; set; }

    public bool PrintConfidence { get; set; }

    public bool NoTimestamps { get; set; }

    public bool LogScore { get; set; }

    public bool NoGpu { get; set; }

    public bool FlashAttention { get; set; } = true;

    public bool SuppressNonSpeechTokens { get; set; }

    public string? SuppressRegex { get; set; }

    public string? Grammar { get; set; }

    public string? GrammarRule { get; set; }

    public double? GrammarPenalty { get; set; }

    #endregion

    #region VAD 配置

    public bool EnableVad { get; set; }

    public string? VadModelPath { get; set; }

    public double? VadThreshold { get; set; }

    public int? VadMinSpeechDurationMs { get; set; }

    public int? VadMinSilenceDurationMs { get; set; }

    public int? VadMaxSpeechDurationS { get; set; }

    public int? VadSpeechPadMs { get; set; }

    public double? VadSamplesOverlap { get; set; }

    #endregion

    #region 其他配置

    public string? FontPath { get; set; }

    public string? OpenVinoDevice { get; set; }

    public string? DtwModel { get; set; }

    #endregion
}
