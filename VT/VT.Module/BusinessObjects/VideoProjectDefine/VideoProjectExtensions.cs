using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VT.Core;

namespace VT.Module.BusinessObjects;

public static class VideoProjectExtensions
{
    /// <summary>
    /// 检测音频文件的语言
    /// </summary>
    /// <param name="audioFilePath">音频文件路径</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>检测到的语言</returns>
    public static async Task<Language> DetectAudioLanguageAsync(string audioFilePath, IServiceProvider serviceProvider)
    {
        var logger = Log.ForContext(typeof(VideoProjectExtensions));
        var whisperService = serviceProvider.GetRequiredService<WhisperRecognitionService>();

        logger.Information("开始检测音频语言: {FilePath}", audioFilePath);

        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioFilePath}");
        }

        var detectedLanguage = await whisperService.DetectLanguageAsync(audioFilePath);
        logger.Information("检测到的语言: {DetectedLanguage}", detectedLanguage);

        return detectedLanguage;
    }

    /// <summary>
    /// 检测视频文件的语言（提取音频后检测）
    /// </summary>
    /// <param name="videoFilePath">视频文件路径</param>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>检测到的语言</returns>
    public static async Task<Language?> DetectVideoLanguageAsync(string videoFilePath, IServiceProvider serviceProvider)
    {
        var logger = Log.ForContext(typeof(VideoProjectExtensions));
        var ffmpegService = serviceProvider.GetRequiredService<IFFmpegService>();
        var whisperService = serviceProvider.GetRequiredService<WhisperRecognitionService>();

        logger.Information("开始检测视频语言: {FilePath}", videoFilePath);

        var tempDir = Path.Combine(Path.GetTempPath(), $"VideoTranslator_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var tempAudioPath = Path.Combine(tempDir, "temp_audio.flac");

            var checkArgs = $"-i \"{videoFilePath}\" -t 0 -f null -";
            var checkOutput = await ffmpegService.ExecuteCommandAsync(checkArgs);

            if (!checkOutput.Contains("Stream #0:0") || !checkOutput.Contains("Audio:"))
            {
                logger.Warning("视频文件没有音频流");
                return null;
            }

            var extractArgs = $"-i \"{videoFilePath}\" -map 0:a:0 -acodec flac -ar 44100 -ac 2 -y \"{tempAudioPath}\"";
            await ffmpegService.ExecuteCommandAsync(extractArgs);

            if (!File.Exists(tempAudioPath))
            {
                logger.Warning("音频提取失败");
                return null;
            }

            var detectedLanguage = await whisperService.DetectLanguageAsync(tempAudioPath);
            logger.Information("检测到的语言: {DetectedLanguage}", detectedLanguage);

            return detectedLanguage;
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// 检测项目音频语言并自动设置源语言和目标语言
    /// </summary>
    /// <param name="videoProject">视频项目</param>
    public static async Task DetectAndSetLanguageAsync(this VideoProject videoProject)
    {
        var logger = Log.ForContext(typeof(VideoProjectExtensions));
        var serviceProvider = videoProject.Session.ServiceProvider;

        logger.Information("开始检测项目音频语言: {ProjectName}", videoProject.ProjectName);

        #region 验证音频文件存在

        if (string.IsNullOrEmpty(videoProject.SourceAudioPath))
        {
            throw new InvalidOperationException("项目音频路径为空，无法检测语言");
        }

        if (!File.Exists(videoProject.SourceAudioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {videoProject.SourceAudioPath}");
        }

        #endregion

        #region 使用 Whisper 检测语言

        var whisperService = serviceProvider.GetRequiredService<WhisperRecognitionService>();
        var detectedLanguage = await whisperService.DetectLanguageAsync(videoProject.SourceAudioPath);

        logger.Information("检测到的语言: {DetectedLanguage}", detectedLanguage);

        #endregion

        #region 根据检测结果设置语言

        if (detectedLanguage != VT.Core.Language.Chinese)
        {
            videoProject.SourceLanguage = detectedLanguage;
            videoProject.TargetLanguage = VT.Core.Language.Chinese;
            logger.Information("检测到非中文语言，设置源语言为: {SourceLanguage}，目标语言为: {TargetLanguage}", 
                videoProject.SourceLanguage, videoProject.TargetLanguage);
        }
        else
        {
            videoProject.SourceLanguage = VT.Core.Language.Chinese;
            videoProject.TargetLanguage = VT.Core.Language.English;
            logger.Information("检测到中文语言，设置源语言为: {SourceLanguage}，目标语言为: {TargetLanguage}", 
                videoProject.SourceLanguage, videoProject.TargetLanguage);
        }

        #endregion
    }

    public static async Task<(AudioSource audio, AudioTrackInfo track)> CreateAudioSourceAndTrackInfo(this VideoProject vp, MediaType type, bool isFullFile, string audioPath)
    {
        //1.创建 AudioSource 对象
        //2.创建 AudioTrackInfo 对象
        var Session = vp.Session;
        var sourceAudio = new AudioSource(Session)
        {
            Name = type.ToString(),
            FileFullName = audioPath,
            MediaType = type
        };
        vp.MediaSources.Add(sourceAudio);

        var sourceAudioTrack = new AudioTrackInfo(Session)
        {
            Media = sourceAudio,
            TrackType = type,
            Title = type.ToString()
        };
        vp.Tracks.Add(sourceAudioTrack);
        if (isFullFile)
        {
            var ac = new AudioClip(Session) { ShowWaveform = true };
            await ac.SetAudioFile(sourceAudio.FileFullName);
            sourceAudioTrack.Segments.Add(ac);
        }
        return (sourceAudio, sourceAudioTrack);
    }
    public static async Task<(SRTSource srt, SRTTrackInfo track)> CreateSubtitleSource(this VideoProject vp, string subtitlePath, MediaType type,VadTrackInfo vad)
    {
        //1.创建 SRTSource 对象
        //2.创建 SRTTrackInfo 对象
        var Session = vp.Session;
        var subtitleFileInfo = new SRTSource(Session)
        {
            FileFullName = subtitlePath,
            MediaType = type
        };

        vp.MediaSources.Add(subtitleFileInfo);
        var srtTrack = new SRTTrackInfo(Session, type, vad);
        srtTrack.Media = subtitleFileInfo;

        var subtitleService = srtTrack.Session.ServiceProvider.GetRequiredService<ISubtitleService>();
        var subtitles = await subtitleService.ParseSrtAsync(subtitlePath);

        int index = 1;
        foreach (var subtitle in subtitles)
        {
            var startTime = subtitle.StartTime;
            var endTime = subtitle.EndTime;
            
            if (endTime < startTime)
            {
                endTime = startTime;
            }
            
            var srtClip = new SRTClip(Session)
            {
                Index = index++,
                Start = startTime,
                End = endTime,
                Text = subtitle.Text
            };
            srtTrack.Segments.Add(srtClip);
        }

        vp.Tracks.Add(srtTrack);
        return (subtitleFileInfo, srtTrack);
    }

    /// <summary>
    /// 处理项目音频的完整工作流
    /// </summary>
    /// <param name="videoProject">视频项目</param>
    /// <param name="regenerate">是否重新生成（清理现有数据）</param>
    public static async Task ProcessProjectAudio(this VideoProject videoProject, bool regenerate = false)
    {
        var logger = Log.ForContext(typeof(VideoProjectExtensions));
        var progressService = videoProject.Session.ServiceProvider.GetService<IProgressService>();

        try
        {
            logger.Information("开始处理项目音频: {ProjectName} (Oid: {Oid}), 重新生成: {Regenerate}", videoProject.ProjectName, videoProject.Oid, regenerate);

            #region 重新生成时的清理逻辑

            if (regenerate)
            {
                logger.Information("开始清理项目数据...");

                #region 清理 Tracks（保留源视频 track）

                var tracksToRemove = videoProject.Tracks
                    .Where(t => t.TrackType != MediaType.时间线)
                    .ToList();

                foreach (var track in tracksToRemove)
                {
                    logger.Information("删除轨道: {TrackType}, Oid: {Oid}", track.TrackType, track.Oid);
                    videoProject.Tracks.Remove(track);
                    track.Delete();
                }

                #endregion

                #region 清理 MediaSources（保留源视频）

                var mediaSourcesToRemove = videoProject.MediaSources
                    .Where(ms => ms.MediaType != MediaType.源视频)
                    .ToList();

                foreach (var mediaSource in mediaSourcesToRemove)
                {
                    logger.Information("删除媒体源: {MediaType}, Oid: {Oid}", mediaSource.MediaType, mediaSource.Oid);

                    #region 删除文件

                    if (mediaSource is AudioSource audioSource && !string.IsNullOrEmpty(audioSource.FileFullName) && File.Exists(audioSource.FileFullName))
                    {
                        try
                        {
                            File.Delete(audioSource.FileFullName);
                            logger.Information("删除音频文件: {FilePath}", audioSource.FileFullName);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex, "删除音频文件失败: {FilePath}", audioSource.FileFullName);
                        }
                    }

                    if (mediaSource is SRTSource srtSource && !string.IsNullOrEmpty(srtSource.FileFullName) && File.Exists(srtSource.FileFullName))
                    {
                        try
                        {
                            File.Delete(srtSource.FileFullName);
                            logger.Information("删除字幕文件: {FilePath}", srtSource.FileFullName);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex, "删除字幕文件失败: {FilePath}", srtSource.FileFullName);
                        }
                    }

                    if (mediaSource is VideoSource videoSource && !string.IsNullOrEmpty(videoSource.FileFullName) && File.Exists(videoSource.FileFullName))
                    {
                        try
                        {
                            File.Delete(videoSource.FileFullName);
                            logger.Information("删除视频文件: {FilePath}", videoSource.FileFullName);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex, "删除视频文件失败: {FilePath}", videoSource.FileFullName);
                        }
                    }

                    #endregion

                    videoProject.MediaSources.Remove(mediaSource);
                    mediaSource.Delete();
                }

                #endregion

                #region 清理 VideoProject 属性

                videoProject.SourceAudioPath = null;
                videoProject.SourceMutedVideoPath = null;
                videoProject.SourceBackgroundAudioPath = null;
                videoProject.SourceSubtitlePath = null;
                videoProject.TranslatedSubtitlePath = null;
                videoProject.OutputAudioPath = null;
                videoProject.OutputVideoPath = null;

                logger.Information("已清理 VideoProject 属性");

                #endregion

                #region 删除项目目录中的临时文件

                var projectPath = videoProject.ProjectPath;
                if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
                {
                    var directoriesToDelete = new[]
                    {
                        Path.Combine(projectPath, "audio_segments"),
                        Path.Combine(projectPath, "tts")
                    };

                    foreach (var dir in directoriesToDelete)
                    {
                        if (Directory.Exists(dir))
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                                logger.Information("删除目录: {DirectoryPath}", dir);
                            }
                            catch (Exception ex)
                            {
                                logger.Warning(ex, "删除目录失败: {DirectoryPath}", dir);
                            }
                        }
                    }

                    var ttsDirectories = Directory.GetDirectories(projectPath, "tts_srt_*");
                    foreach (var dir in ttsDirectories)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            logger.Information("删除TTS目录: {DirectoryPath}", dir);
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex, "删除TTS目录失败: {DirectoryPath}", dir);
                        }
                    }
                }

                #endregion

                videoProject.Save();
                logger.Information("项目数据清理完成");
            }

            #endregion

            #region 获取源音频轨道
            AudioTrackInfo sourceAudioTrack;

            #region 检查是否已有源音频轨道（外部导入的音频）
            var existingAudioTrack = videoProject.Tracks.OfType<AudioTrackInfo>().FirstOrDefault(t => t.TrackType == MediaType.源音频);
            if (existingAudioTrack != null)
            {
                #region 使用已有的音频轨道（外部导入的音频）
                logger.Information("使用外部导入的音频，跳过音频提取");
                sourceAudioTrack = existingAudioTrack;
                #endregion
            }
            else
            {
                #region 从视频中提取音频
                progressService?.SetStatusMessage("正在提取音频...");
                sourceAudioTrack = await videoProject.GetVideoSource().ExtractAudio();
                logger.Information("音频提取完成");
                #endregion
            }
            #endregion
            #endregion

            #region 分离音频（人声、背景）

            progressService?.SetStatusMessage("正在分离音频...");
            var separatedAudio = await sourceAudioTrack.SeparateAudio();
            logger.Information("音频分离完成");

            #endregion

            #region 识别字幕

            progressService?.SetStatusMessage("正在识别字幕...");
            var sourceSrtTrack = await sourceAudioTrack.PurfviewFasterWhisper识别字幕英文();
            logger.Information("字幕识别完成");

            #endregion

            #region 翻译字幕

            progressService?.SetStatusMessage("正在翻译字幕...");
            var targetSubtitle = await sourceSrtTrack.TranslateSRTByLMStudio(MediaType.目标字幕);
            logger.Information("字幕翻译完成");

            #endregion

            #region 分段音频

            progressService?.SetStatusMessage("正在分段音频...");
            var ttsReferencesTrack = await separatedAudio.人声.SegmentSourceAudioBySrt(targetSubtitle);
            logger.Information("音频分段完成");

            #endregion

            #region 生成TTS

            progressService?.SetStatusMessage("正在生成TTS音频...");
            var ttsTrack = await targetSubtitle.GenerateTTS();
            logger.Information("TTS生成完成");

            #endregion

            #region 调整音频

            progressService?.SetStatusMessage("正在调整音频...");
            var adjustedTrack = await ttsTrack.Adjust(true, sourceSrtTrack);
            logger.Information("音频调整完成");

            #endregion

            #region 生成目标音频

            progressService?.SetStatusMessage("正在生成目标音频...");
            var fullTargetAudioTrack = await adjustedTrack.GenerateTargetAudio(separatedAudio.背景);
            logger.Information("目标音频生成完成");

            #endregion

            videoProject.Save();
            logger.Information("项目音频处理完成");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "处理项目音频失败");
            throw;
        }
    }

}