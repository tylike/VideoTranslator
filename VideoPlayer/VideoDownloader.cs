using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using Common.Logging;

namespace VideoPlayer
{
    public class VideoDownloader : IDisposable
    {
        #region 字段

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _media;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposing = false;
        private string _lastErrorMessage = string.Empty;
        private const int MaxParseRetries = 3;
        private const int ParseTimeoutMs = 30000;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<VideoDownloader>();

        #endregion

        #region 事件

        public event EventHandler<DownloadProgressEventArgs> ProgressChanged;
        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;
        public event EventHandler<DownloadErrorEventArgs> DownloadError;

        #endregion

        #region 属性

        public bool IsDownloading { get; private set; }
        public string CurrentDownloadPath { get; private set; }
        public LibVLC LibVLC => _libVLC;

        #endregion

        #region 构造函数

        public VideoDownloader()
        {
            InitializeVLC();
        }

        public VideoDownloader(string[] vlcOptions)
        {
            InitializeVLC(vlcOptions);
        }

        private void InitializeVLC()
        {
            InitializeVLC(new[]
            {
                "--no-xlib",
                "--no-video-title-show",
                "--no-snapshot-preview",
                "--no-audio-time-stretch",
                "--preferred-resolution=1080"
            });
        }

        private void InitializeVLC(string[] vlcOptions)
        {
            Logger.Debug("初始化 VLC 开始");
            Logger.Debug("VLC 选项: {VlcOptions}", string.Join(", ", vlcOptions));
            _libVLC = new LibVLC(vlcOptions);
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.PositionChanged += OnPositionChanged;
            _mediaPlayer.EncounteredError += OnError;
            _mediaPlayer.EndReached += OnEndReached;
            Logger.Debug("VLC 和 MediaPlayer 创建完成");
        }

        private async Task<bool> ParseMediaWithRetryAsync()
        {
            for (int retry = 0; retry < MaxParseRetries; retry++)
            {
                Logger.Debug("解析尝试 {Retry}/{MaxRetries}", retry + 1, MaxParseRetries);

                MediaParsedStatus parseResult = await _media.Parse(MediaParseOptions.ParseNetwork);
                Logger.Debug("解析结果: {ParseResult}", parseResult);
                Logger.Debug("媒体状态: {MediaState}", _media.State);
                Logger.Debug("子项数量: {SubItemCount}", _media.SubItems.Count);

                if (parseResult == MediaParsedStatus.Done)
                {
                    Logger.Debug("解析成功");
                    return true;
                }

                if (parseResult == MediaParsedStatus.Failed)
                {
                    Logger.Debug("解析失败，停止重试");
                    return false;
                }

                if (retry < MaxParseRetries - 1)
                {
                    Logger.Debug("解析超时，等待 {DelayMs}ms 后重试...", 2000);
                    await Task.Delay(2000);
                }
            }

            Logger.Debug("所有解析尝试都失败");
            return false;
        }

        #endregion

        #region 下载方法

        public async Task<bool> DownloadVideoAsync(string sourceUrl, string outputPath, CancellationToken cancellationToken = default)
        {
            if (IsDownloading)
            {
                Debug.WriteLine("[VideoDownloader] 已有下载任务在进行中");
                return false;
            }

            try
            {
                IsDownloading = true;
                CurrentDownloadPath = outputPath;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Logger.Information("开始下载视频: {SourceUrl}", sourceUrl);
                Logger.Information("保存路径: {OutputPath}", outputPath);

                string directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Logger.Debug("创建目录: {Directory}", directory);
                }

                string extension = Path.GetExtension(outputPath).ToLower();
                string soutFormat = GetSoutFormat(extension);
                string finalSoutFormat = soutFormat.Replace("{dst}", outputPath);

                Logger.Debug("文件扩展名: {Extension}", extension);
                Logger.Debug("Sout 格式模板: {SoutFormat}", soutFormat);
                Logger.Debug("最终 Sout 配置: {FinalSoutFormat}", finalSoutFormat);

                _media = new Media(_libVLC, sourceUrl, FromType.FromLocation);
                Logger.Debug("创建 Media 对象: {SourceUrl}", sourceUrl);

                Logger.Debug("开始解析 YouTube URL...");
                bool parseSuccess = await ParseMediaWithRetryAsync();

                if (!parseSuccess)
                {
                    Logger.Warning("解析失败，无法下载");
                    return false;
                }

                if (_media.SubItems.Count == 0)
                {
                    Logger.Warning("没有找到子项，无法下载");
                    return false;
                }

                Media downloadMedia = _media.SubItems.First();
                Logger.Debug("选择子项: {Mrl}", downloadMedia.Mrl);

                var mediaOptions = new string[]
                {
                    $":sout={finalSoutFormat}",
                    ":sout-keep"
                };

                foreach (var option in mediaOptions)
                {
                    Logger.Debug("添加媒体选项: {Option}", option);
                    downloadMedia.AddOption(option);
                }

                _mediaPlayer.Media = downloadMedia;

                Logger.Debug("开始播放（下载）");
                _mediaPlayer.Play();

                await Task.Delay(500, _cancellationTokenSource.Token);
                Logger.Debug("播放器状态: {State}", _mediaPlayer.State);

                int stateCheckCount = 0;
                while (_mediaPlayer.State != VLCState.Ended && 
                       _mediaPlayer.State != VLCState.Error &&
                       !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    stateCheckCount++;
                    if (stateCheckCount % 10 == 0)
                    {
                        Logger.Debug("播放器状态: {State}, 位置: {Position}", _mediaPlayer.State, _mediaPlayer.Position);
                    }
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }

                Logger.Debug("最终状态: {State}", _mediaPlayer.State);
                Logger.Debug("最后错误信息: {LastErrorMessage}", _lastErrorMessage);

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Logger.Information("下载已取消");
                    _mediaPlayer.Stop();
                    return false;
                }

                if (_mediaPlayer.State == VLCState.Error)
                {
                    Logger.Error("下载过程中发生错误");
                    Logger.Error("错误详情: {ErrorMessage}", _lastErrorMessage);
                    return false;
                }

                Logger.Information("下载完成");
                return true;
            }
            catch (OperationCanceledException)
            {
                Logger.Information("下载被取消");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "下载异常");
                return false;
            }
            finally
            {
                IsDownloading = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void CancelDownload()
        {
            if (IsDownloading && _cancellationTokenSource != null)
            {
                Logger.Information("请求取消下载");
                _cancellationTokenSource.Cancel();
            }
        }

        #endregion

        #region 辅助方法

        private string GetSoutFormat(string extension)
        {
            switch (extension)
            {
                case ".mp4":
                    return "#transcode{vcodec=h264,acodec=mpga}:standard{access=file,mux=mp4,dst=\"{dst}\"}";
                case ".mkv":
                    return "#transcode{vcodec=h264,acodec=mpga}:standard{access=file,mux=mkv,dst=\"{dst}\"}";
                case ".avi":
                    return "#transcode{vcodec=h264,acodec=mpga}:standard{access=file,mux=avi,dst=\"{dst}\"}";
                case ".mov":
                    return "#transcode{vcodec=h264,acodec=mpga}:standard{access=file,mux=mp4,dst=\"{dst}\"}";
                default:
                    return "#transcode{vcodec=h264,acodec=mpga}:standard{access=file,mux=mp4,dst=\"{dst}\"}";
            }
        }

        #endregion

        #region 事件处理

        private void OnPositionChanged(object sender, MediaPlayerPositionChangedEventArgs e)
        {
            ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
            {
                Progress = e.Position,
                Percentage = (int)(e.Position * 100)
            });
        }

        private void OnError(object sender, EventArgs e)
        {
            string errorMessage = "下载过程中发生错误";
            
            if (_mediaPlayer != null)
            {
                Debug.WriteLine($"[VideoDownloader] 错误事件触发");
                Debug.WriteLine($"[VideoDownloader] 播放器状态: {_mediaPlayer.State}");
                Debug.WriteLine($"[VideoDownloader] 媒体状态: {_media?.State}");
                
                if (_media != null)
                {
                    Debug.WriteLine($"[VideoDownloader] 媒体 MRL: {_media.Mrl}");
                    Debug.WriteLine($"[VideoDownloader] 媒体类型: {_media.Type}");
                    
                    if (_media.SubItems != null && _media.SubItems.Count > 0)
                    {
                        Debug.WriteLine($"[VideoDownloader] 子项数量: {_media.SubItems.Count}");
                        for (int i = 0; i < _media.SubItems.Count; i++)
                        {
                            Debug.WriteLine($"[VideoDownloader] 子项 {i}: {_media.SubItems[i].Mrl}");
                        }
                    }
                }
            }
            
            _lastErrorMessage = errorMessage;
            Debug.WriteLine($"[VideoDownloader] 错误: {errorMessage}");
            DownloadError?.Invoke(this, new DownloadErrorEventArgs
            {
                ErrorMessage = errorMessage
            });
        }

        private void OnEndReached(object sender, EventArgs e)
        {
            Debug.WriteLine("[VideoDownloader] 下载完成");
            DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
            {
                Success = true,
                OutputPath = CurrentDownloadPath
            });
        }

        #endregion

        #region 资源释放

        public void Dispose()
        {
            if (_isDisposing)
            {
                return;
            }
            _isDisposing = true;

            Logger.Debug("Dispose 开始");
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Stop();
                    _mediaPlayer.PositionChanged -= OnPositionChanged;
                    _mediaPlayer.EncounteredError -= OnError;
                    _mediaPlayer.EndReached -= OnEndReached;
                    _mediaPlayer.Dispose();
                    _mediaPlayer = null;
                }

                if (_media != null)
                {
                    _media.Dispose();
                    _media = null;
                }

                if (_libVLC != null)
                {
                    _libVLC.Dispose();
                    _libVLC = null;
                }

                Logger.Debug("Dispose 完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Dispose 异常");
            }
        }

        #endregion
    }

    #region 事件参数类

    /// <summary>
    /// 下载进度事件参数
    /// </summary>
    public class DownloadProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public int Percentage { get; set; }
    }

    /// <summary>
    /// 下载完成事件参数
    /// </summary>
    public class DownloadCompletedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string OutputPath { get; set; }
    }

    /// <summary>
    /// 下载错误事件参数
    /// </summary>
    public class DownloadErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
    }

    #endregion
}