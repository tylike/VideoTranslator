using System;
using System.Collections.Generic;
using System.Linq;
using VT.Module.BusinessObjects;
using VT.Win.Forms.Models;

namespace VT.Win.Forms.Services
{
    public class TimelineDataService
    {
        public List<TimelineTrackData> GenerateTimelineTracks(List<TimeLineClip> clips, double totalDuration)
        {
            var tracks = new List<TimelineTrackData>();
            var scale = totalDuration > 0 ? 100.0 / totalDuration : 1;

            var sourceSrtTrack = new TimelineTrackData { Label = "源字幕" };
            var sourceAudioTrack = new TimelineTrackData { Label = "源音频" };
            var targetAudioTrack = new TimelineTrackData { Label = "目标音频" };
            var adjustedAudioTrack = new TimelineTrackData { Label = "调整后" };

            foreach (var clip in clips)
            {
                if (clip.SourceSRTClip != null)
                {
                    var start = clip.SourceSRTClip.Start.TotalSeconds;
                    var duration = clip.SourceSRTClip.Duration;
                    sourceSrtTrack.Bars.Add(new TimelineBarData
                    {
                        Index = clip.Index,
                        LeftPercentage = start * scale,
                        WidthPercentage = duration * scale,
                        CssClass = "source-srt",
                        Tooltip = $"片段 #{clip.Index}\n开始: {FormatTime(start)}\n结束: {FormatTime(start + duration)}\n时长: {duration:F2}s\n文本: {clip.SourceSRTClip.Text}"
                    });
                }

                if (clip.SourceAudioClip != null)
                {
                    var start = clip.SourceAudioClip.Start.TotalSeconds;
                    var duration = clip.SourceAudioClip.Duration;
                    sourceAudioTrack.Bars.Add(new TimelineBarData
                    {
                        Index = clip.Index,
                        LeftPercentage = start * scale,
                        WidthPercentage = duration * scale,
                        CssClass = "source-audio",
                        Tooltip = $"片段 #{clip.Index}\n开始: {FormatTime(start)}\n结束: {FormatTime(start + duration)}\n时长: {duration:F2}s"
                    });
                }

                if (clip.TargetAudioClip != null)
                {
                    var start = clip.TargetAudioClip.Start.TotalSeconds;
                    var duration = clip.TargetAudioClip.Duration;
                    targetAudioTrack.Bars.Add(new TimelineBarData
                    {
                        Index = clip.Index,
                        LeftPercentage = start * scale,
                        WidthPercentage = duration * scale,
                        CssClass = "target-audio",
                        Tooltip = $"片段 #{clip.Index}\n开始: {FormatTime(start)}\n结束: {FormatTime(start + duration)}\n时长: {duration:F2}s"
                    });
                }

                if (clip.AdjustedTargetAudioClip != null)
                {
                    var start = clip.AdjustedTargetAudioClip.Start.TotalSeconds;
                    var duration = clip.AdjustedTargetAudioClip.Duration;
                    adjustedAudioTrack.Bars.Add(new TimelineBarData
                    {
                        Index = clip.Index,
                        LeftPercentage = start * scale,
                        WidthPercentage = duration * scale,
                        CssClass = "adjusted-audio",
                        Tooltip = $"片段 #{clip.Index}\n开始: {FormatTime(start)}\n结束: {FormatTime(start + duration)}\n时长: {duration:F2}s"
                    });
                }
            }

            tracks.Add(sourceSrtTrack);
            tracks.Add(sourceAudioTrack);
            tracks.Add(targetAudioTrack);
            tracks.Add(adjustedAudioTrack);

            return tracks;
        }

        public List<SubtitleTrack> GenerateSubtitleTracks(VideoProject project)
        {
            var tracks = new List<SubtitleTrack>();

            if (!string.IsNullOrEmpty(project.SourceSubtitlePath))
            {
                tracks.Add(new SubtitleTrack
                {
                    Name = "中文字幕",
                    Language = "zh",
                    Color = Color.FromArgb(76, 175, 80),
                    Subtitles = new List<SubtitleItem>()
                });
            }

            if (!string.IsNullOrEmpty(project.TranslatedSubtitlePath))
            {
                tracks.Add(new SubtitleTrack
                {
                    Name = "英文字幕",
                    Language = "en",
                    Color = Color.FromArgb(33, 150, 243),
                    Subtitles = new List<SubtitleItem>()
                });
            }

            return tracks;
        }

        public List<SubtitleTrack> GenerateSampleSubtitleTracks()
        {
            return new List<SubtitleTrack>
            {
                new SubtitleTrack
                {
                    Name = "中文字幕",
                    Language = "zh",
                    Color = Color.FromArgb(76, 175, 80),
                    Subtitles = new List<SubtitleItem>()
                },
                new SubtitleTrack
                {
                    Name = "英文字幕",
                    Language = "en",
                    Color = Color.FromArgb(33, 150, 243),
                    Subtitles = new List<SubtitleItem>()
                }
            };
        }

        public List<TimelineTrackData> GenerateSampleTimelineTracks()
        {
            var tracks = new List<TimelineTrackData>();

            var sourceSrtTrack = new TimelineTrackData
            {
                Label = "源字幕",
                Bars = new List<TimelineBarData>
                {
                    new TimelineBarData { Index = 1, LeftPercentage = 0, WidthPercentage = 10, CssClass = "source-srt", Tooltip = "开始: 00:00\n结束: 00:10\n文本: 示例字幕1" },
                    new TimelineBarData { Index = 2, LeftPercentage = 12, WidthPercentage = 8, CssClass = "source-srt", Tooltip = "开始: 00:12\n结束: 00:20\n文本: 示例字幕2" },
                    new TimelineBarData { Index = 3, LeftPercentage = 22, WidthPercentage = 15, CssClass = "source-srt", Tooltip = "开始: 00:22\n结束: 00:37\n文本: 示例字幕3" }
                }
            };

            var sourceAudioTrack = new TimelineTrackData
            {
                Label = "源音频",
                Bars = new List<TimelineBarData>
                {
                    new TimelineBarData { Index = 1, LeftPercentage = 0, WidthPercentage = 37, CssClass = "source-audio", Tooltip = "开始: 00:00\n结束: 00:37\n时长: 37秒" }
                }
            };

            var targetAudioTrack = new TimelineTrackData
            {
                Label = "目标音频",
                Bars = new List<TimelineBarData>
                {
                    new TimelineBarData { Index = 1, LeftPercentage = 0, WidthPercentage = 35, CssClass = "target-audio", Tooltip = "开始: 00:00\n结束: 00:35\n时长: 35秒" }
                }
            };

            var adjustedAudioTrack = new TimelineTrackData
            {
                Label = "调整后",
                Bars = new List<TimelineBarData>
                {
                    new TimelineBarData { Index = 1, LeftPercentage = 0, WidthPercentage = 37, CssClass = "adjusted-audio", Tooltip = "开始: 00:00\n结束: 00:37\n时长: 37秒" }
                }
            };

            tracks.Add(sourceSrtTrack);
            tracks.Add(sourceAudioTrack);
            tracks.Add(targetAudioTrack);
            tracks.Add(adjustedAudioTrack);

            return tracks;
        }

        private string FormatTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}:{2:D2}",
                timeSpan.Hours,
                timeSpan.Minutes,
                timeSpan.Seconds);
        }
    }
}
