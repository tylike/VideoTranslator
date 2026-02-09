using System;
using System.IO;
using VideoTranslator.Models;
using VideoTranslator.SRT.Core.Models;
using VideoTranslator.Utils;
using VT.Core;

namespace VT.Module.BusinessObjects;

public static class ClipExtensions
{
    public static ISrtSubtitle ToSubtitle(this Clip clip)
    {
        var srtClip = clip as SRTClip;
        var text = srtClip?.Text ?? string.Empty;        
        return new SrtSubtitle(clip.Index, clip.Start, clip.End, text);        
    }

    public static string EscapeForFFmpeg(this string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var fullPath = Path.GetFullPath(path);
        return fullPath.Replace("\\", "\\\\").Replace(":", "\\:");
    }
}
