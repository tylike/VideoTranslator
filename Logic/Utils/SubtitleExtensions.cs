using VideoTranslator.Models;
using VideoTranslator.SRT.Core.Extensions;
using VideoTranslator.SRT.Core.Models;

namespace VideoTranslator.Utils;

public static class SubtitleExtensions
{
    #region SrtSubtitle 转 Subtitle

    //public static Models.Subtitle ToSubtitle(this SRT.Core.Models.SrtSubtitle srtSubtitle)
    //{
    //    return new Models.Subtitle
    //    {
    //        Index = srtSubtitle.Index,
    //        StartTime = srtSubtitle.StartTime.ToSrtTimeString(),
    //        EndTime = srtSubtitle.EndTime.ToSrtTimeString(),
    //        Text = srtSubtitle.Text,
    //        TimeRange = $"{srtSubtitle.StartTime.ToSrtTimeString()} --> {srtSubtitle.EndTime.ToSrtTimeString()}"
    //    };
    //}

    //public static List<Models.Subtitle> ToSubtitleList(this IEnumerable<SRT.Core.Models.SrtSubtitle> srtSubtitles)
    //{
    //    return srtSubtitles.Select(s => s.ToSubtitle()).ToList();
    //}

    //public static List<Models.Subtitle> ToSubtitleList(this SrtFile srtFile)
    //{
    //    return srtFile.Subtitles.ToSubtitleList();
    //}

    #endregion

    #region Subtitle 转 SrtSubtitle

    //public static SRT.Core.Models.SrtSubtitle ToSrtSubtitle(this Models.Subtitle subtitle)
    //{
    //    return new SRT.Core.Models.SrtSubtitle
    //    {
    //        Index = subtitle.Index,
    //        StartTime = TimeSpan.FromSeconds(subtitle.StartSeconds),
    //        EndTime = TimeSpan.FromSeconds(subtitle.EndSeconds),
    //        Text = subtitle.Text
    //    };
    //}

    //public static List<SRT.Core.Models.SrtSubtitle> ToSrtSubtitleList(this IEnumerable<Models.Subtitle> subtitles)
    //{
    //    return subtitles.Select(s => s.ToSrtSubtitle()).ToList();
    //}

    //public static SrtFile ToSrtFile(this IEnumerable<Models.Subtitle> subtitles)
    //{
    //    return new SrtFile(subtitles.ToSrtSubtitleList());
    //}

    #endregion
}