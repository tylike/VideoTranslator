using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VideoTranslator.Models;
using VideoTranslator.Utils;

namespace VT.Module.BusinessObjects;

public partial class VideoProject
{
    public void CreateDebugSubtitle(string outputPath)
    {
        try
        {
            var clips = Clips?.OrderBy(c => c.Index).ToList();

            if (clips == null || clips.Count == 0)
            {
                Debug.WriteLine("[CreateDebugSubtitle] Clips 列表为空");
                return;
            }

            var srtLines = new List<string>();
            int index = 1;

            foreach (var clip in clips)
            {
                var startTime = clip.SourceSRTClip.Start.TotalSeconds.ToSrtTimeString();
                var endTime = clip.SourceSRTClip.End.TotalSeconds.ToSrtTimeString();

                var audioAdjusted = clip.AudioAdjusted ? "Yes" : "No";
                var speedMultiplier = clip.SpeedMultiplier.ToString("F2");
                var debugText = $"Clip: {clip.Index}, Audio: {audioAdjusted}, Speed: {speedMultiplier}";

                srtLines.Add(index.ToString());
                srtLines.Add($"{startTime} --> {endTime}");
                srtLines.Add(debugText);
                srtLines.Add("");

                index++;
            }

            File.WriteAllLines(outputPath, srtLines);
            Debug.WriteLine($"[CreateDebugSubtitle] 调试字幕生成成功: {outputPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CreateDebugSubtitle] 生成调试字幕失败: {ex.Message}");
            throw;
        }
    }
}
