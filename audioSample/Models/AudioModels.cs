using System.Diagnostics;
using System.Text.Json;

namespace AudioSample.Models
{
    #region 基础数据模型

    public class AudioSegment
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
        public int? SpeakerId { get; set; }
        public List<WordTimestamp> WordTimestamps { get; set; } = new();
        public double Duration => EndTime - StartTime;
    }

    public class WordTimestamp
    {
        public string Word { get; set; } = string.Empty;
        public double StartTime { get; set; }
        public double EndTime { get; set; }
    }

    public class VADSegment
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Duration => EndTime - StartTime;
    }

    public class SpeakerSegment
    {
        public int SpeakerId { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public double Duration => EndTime - StartTime;
    }

    #endregion

    #region Whisper 结果模型

    public class WhisperResult
    {
        public string Text { get; set; } = string.Empty;
        public List<WhisperSegment> Segments { get; set; } = new();
    }

    public class WhisperSegment
    {
        public int Id { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<WordTimestamp>? Words { get; set; }
    }

    #endregion

    #region Sherpa-ONNX 结果模型

    public class SherpaVADResult
    {
        public List<VADSegment> Segments { get; set; } = new();
    }

    public class SherpaSpeakerResult
    {
        public List<SpeakerSegment> Segments { get; set; } = new();
    }

    public class SherpaTranscriptionResult
    {
        public string Text { get; set; } = string.Empty;
        public List<WordTimestamp> WordTimestamps { get; set; } = new();
    }

    #endregion

    #region 整合结果模型

    public class IntegratedTranscriptionResult
    {
        public string FullText { get; set; } = string.Empty;
        public List<AudioSegment> Segments { get; set; } = new();
        public int SpeakerCount { get; set; }
        public double TotalDuration { get; set; }
    }

    #endregion
}
