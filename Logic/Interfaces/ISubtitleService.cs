using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VideoTranslator.Interfaces;

public interface ISubtitleService
{
    Task<List<ISrtSubtitle>> ParseSrtAsync(string srtPath);
    Task SaveSrtAsync(List<ISrtSubtitle> subtitles, string outputPath);
}
