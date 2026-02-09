using DevExpress.Xpo;
using Serilog;
using System.Diagnostics;
using System.Windows;
using TrackMenuAttributes;

namespace VT.Module.BusinessObjects;

public class VideoSource(Session s) : MediaSource(s)
{
    private readonly ILogger _logger = Log.ForContext<VideoSource>();

    #region 上下文菜单操作



    [ContextMenuAction("提取音频", Order = 20, Group = "处理",IsAutoCommit = true)]
    public async Task<AudioTrackInfo> ExtractAudio()
    {
        try
        {
            IServices s = this;
            var videoProject = this.VideoProject;
            videoProject.ValidateSourceVideo();
            var audioPath = videoProject.CombinePath("source_audio.flac");
            var mutedVideoPath = videoProject.CombinePath("source_muted_video.mp4");

            var progressService = s.ProgressService;
            if (progressService == null)
            {
                _logger.Warning("ProgressService is null! ServiceProvider: {ServiceProvider}", s.ServiceProvider);
            }

            progressService?.ShowProgress();
            progressService?.SetStatusMessage("正在检查视频流信息...");

            var checkArgs = $"-i \"{videoProject.SourceVideoPath}\" -t 0 -f null -";
            var checkOutput = await s.FfmpegService.ExecuteCommandAsync(checkArgs);

            if (!checkOutput.Contains("Stream #0:0") || !checkOutput.Contains("Audio:"))
            {
                _logger.Warning("视频文件没有音频流，跳过音频提取");
                progressService?.SetStatusMessage("视频没有音频流，跳过音频提取");

                var mutedVideoArgs = $"-i \"{videoProject.SourceVideoPath}\" -map 0:v:0 -c:v copy -y \"{mutedVideoPath}\"";
                await s.FfmpegService.ExecuteCommandAsync(mutedVideoArgs);

                videoProject.SourceMutedVideoPath = mutedVideoPath;

                var mutedVideoSourceNoAudio = new VideoSource(Session)
                {
                    Name = "静音视频",
                    FileFullName = mutedVideoPath,
                    MediaType = MediaType.静音视频
                };
                this.VideoProject.MediaSources.Add(mutedVideoSourceNoAudio);

                progressService?.ResetProgress();
                return null;
            }

            progressService?.SetStatusMessage("正在提取音频和静音视频...");

            var args = $"-i \"{videoProject.SourceVideoPath}\" -map 0:v:0 -c:v copy -y \"{mutedVideoPath}\" -map 0:a:0 -acodec flac -ar 44100 -ac 2 -y \"{audioPath}\"";
            await s.FfmpegService.ExecuteCommandAsync(args);

            videoProject.SourceAudioPath = audioPath;
            videoProject.SourceMutedVideoPath = mutedVideoPath;

            var rst = await videoProject.CreateAudioSourceAndTrackInfo(MediaType.源音频, true, audioPath);

            #region 静音视频
            var mutedVideoSource = new VideoSource(Session)
            {
                Name = "静音视频",
                FileFullName = mutedVideoPath,
                MediaType = MediaType.静音视频
            };
            this.VideoProject.MediaSources.Add(mutedVideoSource); 
            #endregion

            progressService?.ResetProgress();
            return rst.track;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "提取音频失败");
            throw;
        }
    }

    #endregion
}
