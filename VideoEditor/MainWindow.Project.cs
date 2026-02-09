using System.IO;
using System.Windows;
using TimeLine.Models;
using VT.Module.BusinessObjects;

namespace VideoEditor;

public partial class MainWindow
{
    private void LoadProject(VideoProject videoProject)
    {
        try
        {
            _logger.Information("开始加载项目: {ProjectName} (Oid: {Oid})", videoProject.ProjectName, videoProject.Oid);

            #region 订阅项目对象变更事件

            if (_currentProject != null && _currentProject != videoProject)
            {
                _currentProject.Changed -= OnObjectChanged;
                UnsubscribeTracksCollectionChanged(_currentProject);
            }

            _currentProject = videoProject;
            
            _currentProject.Changed += OnObjectChanged;
            SubscribeTracksCollectionChanged(_currentProject);
            _logger.Information("已订阅 VideoProject 对象变更事件");

            #endregion

            #region 加载时间线

            timeLinePanel.Tracks = videoProject.Tracks;
            timeLinePanel.ZoomFactor = 0.1;
            
            _logger.Information("项目加载完成");

            #endregion

            #region 加载视频和字幕

            if (!string.IsNullOrEmpty(videoProject.SourceVideoPath) && File.Exists(videoProject.SourceVideoPath))
            {
                LoadVideo(videoProject.SourceVideoPath);
                _logger.Information("已加载项目视频: {VideoPath}", videoProject.SourceVideoPath);
            }

            #endregion

            #region 初始化字幕列表

            UpdateSubtitleList();

            #endregion

            #region 更新播放模式

            UpdateVideoPlayerPlayModes(videoProject);

            #endregion
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载项目失败");
            MessageBox.Show($"加载项目失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private async Task ProcessProjectAudio(VideoProject videoProject, bool regenerate = false)
    {
        try
        {
            _logger.Information("开始处理项目音频: {ProjectName} (Oid: {Oid}), 重新生成: {Regenerate}", videoProject.ProjectName, videoProject.Oid, regenerate);

            #region 重新生成时的清理逻辑

            if (regenerate)
            {
                _logger.Information("开始清理项目数据...");

                #region 清理 Tracks（保留源视频 track）

                var tracksToRemove = videoProject.Tracks
                    .Where(t => t.TrackType != MediaType.时间线)
                    .ToList();

                foreach (var track in tracksToRemove)
                {
                    _logger.Information("删除轨道: {TrackType}, Oid: {Oid}", track.TrackType, track.Oid);
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
                    _logger.Information("删除媒体源: {MediaType}, Oid: {Oid}", mediaSource.MediaType, mediaSource.Oid);

                    #region 删除文件

                    if (mediaSource is AudioSource audioSource && !string.IsNullOrEmpty(audioSource.FileFullName) && File.Exists(audioSource.FileFullName))
                    {
                        try
                        {
                            File.Delete(audioSource.FileFullName);
                            _logger.Information("删除音频文件: {FilePath}", audioSource.FileFullName);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "删除音频文件失败: {FilePath}", audioSource.FileFullName);
                        }
                    }

                    if (mediaSource is SRTSource srtSource && !string.IsNullOrEmpty(srtSource.FileFullName) && File.Exists(srtSource.FileFullName))
                    {
                        try
                        {
                            File.Delete(srtSource.FileFullName);
                            _logger.Information("删除字幕文件: {FilePath}", srtSource.FileFullName);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "删除字幕文件失败: {FilePath}", srtSource.FileFullName);
                        }
                    }

                    if (mediaSource is VideoSource videoSource && !string.IsNullOrEmpty(videoSource.FileFullName) && File.Exists(videoSource.FileFullName))
                    {
                        try
                        {
                            File.Delete(videoSource.FileFullName);
                            _logger.Information("删除视频文件: {FilePath}", videoSource.FileFullName);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "删除视频文件失败: {FilePath}", videoSource.FileFullName);
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

                _logger.Information("已清理 VideoProject 属性");

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
                                _logger.Information("删除目录: {DirectoryPath}", dir);
                            }
                            catch (Exception ex)
                            {
                                _logger.Warning(ex, "删除目录失败: {DirectoryPath}", dir);
                            }
                        }
                    }

                    var ttsDirectories = Directory.GetDirectories(projectPath, "tts_srt_*");
                    foreach (var dir in ttsDirectories)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            _logger.Information("删除TTS目录: {DirectoryPath}", dir);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "删除TTS目录失败: {DirectoryPath}", dir);
                        }
                    }
                }

                #endregion

                _objectSpace.CommitChanges();
                _logger.Information("项目数据清理完成");
            }

            #endregion

            #region 获取源音频轨道
            AudioTrackInfo sourceAudioTrack;

            #region 检查是否已有源音频轨道（外部导入的音频）
            var existingAudioTrack = videoProject.Tracks.OfType<AudioTrackInfo>().FirstOrDefault(t => t.TrackType == MediaType.源音频);
            if (existingAudioTrack != null)
            {
                #region 使用已有的音频轨道（外部导入的音频）
                _logger.Information("使用外部导入的音频，跳过音频提取");
                sourceAudioTrack = existingAudioTrack;
                #endregion
            }
            else
            {
                #region 从视频中提取音频
                _logger.Information("从视频中提取音频");
                sourceAudioTrack = await videoProject.GetVideoSource().ExtractAudio();
                #endregion
            }
            #endregion
            #endregion

            #region 分离音频，人声，背景
            var sbt = await sourceAudioTrack.SeparateAudio();
            #endregion

            //使用人声得到vad
            //var vadTrackNoMerge = sbt.人声.GenerateVadNoMerge(); //videoProject.Tracks.OfType<VadTrackInfo>().First();
            //使用vad识别字幕

            var sourceSrtTrack = await sourceAudioTrack.PurfviewFasterWhisper识别字幕(videoProject.SourceLanguage);

            //合并vad字幕段落
            //var vadSrtTrackMerged = await sourceSrtTrack.MergeSegment(MediaType.合并后字幕);
            //翻译字幕
            var targetSubtitle = await sourceSrtTrack.TranslateSRTByLMStudio( MediaType.目标字幕 );

            var ttsReferencesTrack = await sbt.人声.SegmentSourceAudioBySrt(targetSubtitle);

            var ttsTrack = await targetSubtitle.GenerateTTS();
            var adjustedTrack = await ttsTrack.Adjust(true,sourceSrtTrack);
            var fullTargetAudioTrack = await adjustedTrack.GenerateTargetAudio(sbt.背景);
            _logger.Information("项目音频处理完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "处理项目音频失败");
            MessageBox.Show($"处理项目音频失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            this._objectSpace.CommitChanges();
        }
    }

    #region 字幕轨道管理

    /// <summary>
    /// 订阅 Tracks 集合变更事件
    /// </summary>
    private void SubscribeTracksCollectionChanged(VideoProject videoProject)
    {
        if (videoProject != null && videoProject.Tracks != null)
        {
            videoProject.Tracks.CollectionChanged += OnTracksCollectionChanged;
            _logger.Information("已订阅 Tracks 集合变更事件");
        }
    }

    /// <summary>
    /// 取消订阅 Tracks 集合变更事件
    /// </summary>
    private void UnsubscribeTracksCollectionChanged(VideoProject videoProject)
    {
        if (videoProject != null && videoProject.Tracks != null)
        {
            videoProject.Tracks.CollectionChanged -= OnTracksCollectionChanged;
            _logger.Information("已取消订阅 Tracks 集合变更事件");
        }
    }

    /// <summary>
    /// Tracks 集合变更事件处理
    /// </summary>
    private void OnTracksCollectionChanged(object? sender, DevExpress.Xpo.XPCollectionChangedEventArgs e)
    {
        try
        {
            _logger.Information("Tracks 集合发生变更: CollectionChangedType={CollectionChangedType}", e.CollectionChangedType);

            UpdateSubtitleList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "处理 Tracks 集合变更事件失败");
        }
    }

    /// <summary>
    /// 更新字幕列表
    /// </summary>
    private void UpdateSubtitleList()
    {
        if (_currentProject == null)
        {
            return;
        }

        try
        {
            _logger.Information("开始更新字幕列表");

            var subtitleTracks = _currentProject.Tracks
                .Where(t => t is SRTTrackInfo)
                .Cast<SRTTrackInfo>()
                .ToList();

            _logger.Information("找到 {Count} 个字幕轨道", subtitleTracks.Count);

            var subtitleFiles = new List<VideoPlayer.SubtitleFileInfo>();

            foreach (var track in subtitleTracks)
            {
                if (track.Media is SRTSource srtSource && 
                    !string.IsNullOrEmpty(srtSource.FileFullName) && 
                    File.Exists(srtSource.FileFullName))
                {
                    subtitleFiles.Add(new VideoPlayer.SubtitleFileInfo
                    {
                        FilePath = srtSource.FileFullName,
                        DisplayName = GetSubtitleDisplayName(track.TrackType)
                    });

                    _logger.Information("添加字幕: {TrackType} -> {FilePath}", track.TrackType, srtSource.FileFullName);
                }
            }

            videoPlayerControl.UpdateSubtitleList(subtitleFiles);

            _logger.Information("字幕列表更新完成，共 {Count} 个字幕", subtitleFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "更新字幕列表失败");
        }
    }

    /// <summary>
    /// 获取字幕显示名称
    /// </summary>
    private string GetSubtitleDisplayName(MediaType trackType)
    {
        return trackType switch
        {
            MediaType.源字幕 => "源字幕",
            MediaType.目标字幕 => "目标字幕",
            MediaType.源字幕Vad => "源字幕(VAD)",
            MediaType.源字幕下载 => "源字幕(下载)",
            _ => trackType.ToString()
        };
    }

    #endregion

}

