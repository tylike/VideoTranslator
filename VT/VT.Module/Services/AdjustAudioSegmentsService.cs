using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VT.Module.BusinessObjects;
using VT.Module.Controllers;

namespace VT.Module.Services;


public class ClipAudioInfo
{
    public int Index { get; set; }
    public string? SourceAudioPath { get; set; }
    public TimeSpan SourceStart { get; set; }
    public TimeSpan SourceEnd { get; set; }
    public string TargetAudioPath { get; set; } = string.Empty;
    public TimeSpan TargetStart { get; set; }
    public TimeSpan TargetEnd { get; set; }
    public Action<string>? SetAudioFileCallback { get; set; }
}


