using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using VideoTranslator.Config;
using Serilog;
using VideoTranslator.Models;

namespace VideoEditor.Helpers;

public static class VideoSplitHelper
{
    #region 视频分割

    public static List<string> SplitVideoBySegments(
        string ffmpegPath,
        string videoPath,
        List<VadSegment> segments,
        string outputFolder,
        string outputPrefix = "segment")
    {
        var outputFiles = new List<string>();

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        foreach (var segment in segments.Where(s => s.IsSpeech))
        {
            var outputFile = Path.Combine(outputFolder, $"{outputPrefix}_{segment.Index:D3}.mp4");
            var duration = segment.End - segment.Start;

            var arguments = $"-ss {segment.Start} -i \"{videoPath}\" -t {duration} -c:v libx264 -c:a aac -y \"{outputFile}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0 && File.Exists(outputFile))
            {
                outputFiles.Add(outputFile);
            }
        }

        return outputFiles;
    }

    #endregion
}
