using System;
using System.IO;
using System.Linq;
using VT.Module.BusinessObjects;
using VT.Win.Forms.Models;

namespace VT.Win.Forms.Services
{
    public class ProjectLoader
    {
        private TimelineDataService _timelineDataService;

        public ProjectLoader()
        {
            _timelineDataService = new TimelineDataService();
        }

        public ProjectLoadResult LoadProject(VideoProject project)
        {
            var result = new ProjectLoadResult();

            if (project == null)
            {
                result.Success = false;
                result.ErrorMessage = "项目为空";
                return result;
            }

            result.ProjectName = project.ProjectName;
            result.VideoPath = project.SourceVideoPath;

            if (!string.IsNullOrEmpty(project.SourceVideoPath) && File.Exists(project.SourceVideoPath))
            {
                result.ShouldLoadVideo = true;
                result.VideoPath = project.SourceVideoPath;
            }

            result.SubtitleTracks = _timelineDataService.GenerateSubtitleTracks(project);

            var clips = project.Clips.OrderBy(c => c.Index).ToList();
            if (clips.Count > 0)
            {
                var totalDuration = clips.Max(c => c.SourceSRTClip?.End.Ticks ?? 0) / 10000000.0;
                result.TotalDuration = totalDuration;
                result.TimelineTracks = _timelineDataService.GenerateTimelineTracks(clips, totalDuration);
            }

            result.Success = true;
            return result;
        }

        public ProjectLoadResult LoadSampleData()
        {
            var result = new ProjectLoadResult
            {
                ProjectName = "示例项目",
                VideoPath = null,
                ShouldLoadVideo = false,
                TotalDuration = 100,
                SubtitleTracks = _timelineDataService.GenerateSampleSubtitleTracks(),
                TimelineTracks = _timelineDataService.GenerateSampleTimelineTracks(),
                Success = true
            };

            return result;
        }
    }

    public class ProjectLoadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ProjectName { get; set; }
        public string VideoPath { get; set; }
        public bool ShouldLoadVideo { get; set; }
        public double TotalDuration { get; set; }
        public System.Collections.Generic.List<SubtitleTrack> SubtitleTracks { get; set; }
        public System.Collections.Generic.List<TimelineTrackData> TimelineTracks { get; set; }
    }
}
