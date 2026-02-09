using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VideoTranslator.Services;

//public interface ISegmentSourceSRTService
//{
//    Task<SegmentResult> SegmentAsync(SegmentContext context);
//}

//public class SegmentContext
//{
//    public string SourceSubtitlePath { get; set; } = string.Empty;
//    public string SourceAudioPath { get; set; } = string.Empty;
//    public string SourceVocalAudioPath { get; set; } = string.Empty;
//    public string ProjectPath { get; set; } = string.Empty;
//    public IProgress<ProgressInfo>? Progress { get; set; }
//    //public Action<string, (string name, object value)[]>? LogOperation { get; set; }
//}

//public class SegmentResult
//{
//    public List<ClipInfo> Clips { get; set; } = new();
//    public int SubtitleCount { get; set; }
//    public bool Success { get; set; }
//    public string? ErrorMessage { get; set; }
//}

//public class ClipInfo
//{
//    public int Index { get; set; }
//    public string SrtFilePath { get; set; } = string.Empty;
//    public string AudioFilePath { get; set; } = string.Empty;
//    public double StartSeconds { get; set; }
//    public double EndSeconds { get; set; }
//    public string Text { get; set; } = string.Empty;
//}

//public class SegmentSourceSRTService : ServiceBase, ISegmentSourceSRTService
//{
//    private readonly ISubtitleService _subtitleService;
//    private readonly IAudioService _audioService;

//    public SegmentSourceSRTService(ISubtitleService subtitleService, IAudioService audioService)
//    {
//        _subtitleService = subtitleService;
//        _audioService = audioService;
//    }


//}
