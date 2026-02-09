using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Common.Logging;

namespace VideoPlayer
{
    /// <summary>
    /// 播放模式枚举
    /// </summary>
    public enum PlayMode
    {
        /// <summary>
        /// 播放原始视频
        /// </summary>
        OriginalVideo,
        /// <summary>
        /// 播放静音视频+合并后音频（双播放器同时播放）
        /// </summary>
        MutedVideoWithAudio,
        /// <summary>
        /// 播放最终视频
        /// </summary>
        FinalVideo
    }

    /// <summary>
    /// VideoPlayerControl.xaml 的交互逻辑
    /// </summary>
    public partial class VideoPlayerControl : UserControl, IDisposable
    {
        #region 字段

        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _media;
        private bool _isDragging = false;
        private DispatcherTimer _timer;
        private bool _isDisposing = false;
        private static readonly Serilog.ILogger Logger = Common.Logging.LoggerService.ForContext<VideoPlayerControl>();

        // 双播放器模式：用于同时播放静音视频和音频
        private MediaPlayer _audioPlayer;
        private Media _audioMedia;
        private bool _isDualPlayerMode = false;

        // 字幕文件列表
        private List<SubtitleFileInfo> _subtitleFiles = new List<SubtitleFileInfo>();

        // 当前选中的字幕索引
        private int _currentSubtitleIndex = -1;

        // 当前选中的字幕路径
        private string? _currentSubtitlePath = null;

        // 字幕索引到 SPU ID 的映射
        private Dictionary<int, int> _subtitleIndexToSpuId = new Dictionary<int, int>();

        #endregion

        #region 依赖属性

        /// <summary>
        /// 当前播放模式
        /// </summary>
        public static readonly DependencyProperty CurrentPlayModeProperty =
            DependencyProperty.Register(nameof(CurrentPlayMode), typeof(PlayMode), typeof(VideoPlayerControl),
                new PropertyMetadata(PlayMode.OriginalVideo, OnCurrentPlayModeChanged));

        public PlayMode CurrentPlayMode
        {
            get { return (PlayMode)GetValue(CurrentPlayModeProperty); }
            set { SetValue(CurrentPlayModeProperty, value); }
        }

        /// <summary>
        /// 原始视频路径
        /// </summary>
        public static readonly DependencyProperty OriginalVideoPathProperty =
            DependencyProperty.Register(nameof(OriginalVideoPath), typeof(string), typeof(VideoPlayerControl),
                new PropertyMetadata(string.Empty));

        public string OriginalVideoPath
        {
            get { return (string)GetValue(OriginalVideoPathProperty); }
            set { SetValue(OriginalVideoPathProperty, value); }
        }

        /// <summary>
        /// 静音视频路径
        /// </summary>
        public static readonly DependencyProperty MutedVideoPathProperty =
            DependencyProperty.Register(nameof(MutedVideoPath), typeof(string), typeof(VideoPlayerControl),
                new PropertyMetadata(string.Empty));

        public string MutedVideoPath
        {
            get { return (string)GetValue(MutedVideoPathProperty); }
            set { SetValue(MutedVideoPathProperty, value); }
        }

        /// <summary>
        /// 合并后音频路径
        /// </summary>
        public static readonly DependencyProperty MergedAudioPathProperty =
            DependencyProperty.Register(nameof(MergedAudioPath), typeof(string), typeof(VideoPlayerControl),
                new PropertyMetadata(string.Empty));

        public string MergedAudioPath
        {
            get { return (string)GetValue(MergedAudioPathProperty); }
            set { SetValue(MergedAudioPathProperty, value); }
        }

        /// <summary>
        /// 最终视频路径
        /// </summary>
        public static readonly DependencyProperty FinalVideoPathProperty =
            DependencyProperty.Register(nameof(FinalVideoPath), typeof(string), typeof(VideoPlayerControl),
                new PropertyMetadata(string.Empty));

        public string FinalVideoPath
        {
            get { return (string)GetValue(FinalVideoPathProperty); }
            set { SetValue(FinalVideoPathProperty, value); }
        }

        /// <summary>
        /// 播放模式可用状态
        /// </summary>
        public static readonly DependencyProperty PlayModesAvailableProperty =
            DependencyProperty.Register(nameof(PlayModesAvailable), typeof(PlayMode[]), typeof(VideoPlayerControl),
                new PropertyMetadata(new[] { PlayMode.OriginalVideo }, OnPlayModesAvailableChanged));

        public PlayMode[] PlayModesAvailable
        {
            get { return (PlayMode[])GetValue(PlayModesAvailableProperty); }
            set { SetValue(PlayModesAvailableProperty, value); }
        }

        /// <summary>
        /// 播放模式不可用原因
        /// </summary>
        public static readonly DependencyProperty PlayModeUnavailableReasonsProperty =
            DependencyProperty.Register(nameof(PlayModeUnavailableReasons), typeof(Dictionary<PlayMode, string>), typeof(VideoPlayerControl),
                new PropertyMetadata(new Dictionary<PlayMode, string>(), OnPlayModeUnavailableReasonsChanged));

        public Dictionary<PlayMode, string> PlayModeUnavailableReasons
        {
            get { return (Dictionary<PlayMode, string>)GetValue(PlayModeUnavailableReasonsProperty); }
            set { SetValue(PlayModeUnavailableReasonsProperty, value); }
        }

        #endregion

        #region 依赖属性回调

        private static void OnCurrentPlayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VideoPlayerControl control)
            {
                control.OnPlayModeChanged((PlayMode)e.NewValue);
            }
        }

        private static void OnPlayModesAvailableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VideoPlayerControl control)
            {
                control.UpdatePlayModeButtons();
            }
        }

        private static void OnPlayModeUnavailableReasonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VideoPlayerControl control)
            {
                control.UpdatePlayModeButtons();
            }
        }

        #endregion

        #region 构造函数和初始化

        public VideoPlayerControl()
        {
            InitializeComponent();
            InitializeVLC();
            InitializeTimer();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePlayModeButtons();
        }

        private void InitializeVLC()
        {
            Logger.Debug("初始化 VLC 开始");
            
            Core.Initialize();
            
            _libVLC = new LibVLC(
                "--no-xlib",
                "--no-video-title-show",
                "--no-snapshot-preview",
                "--no-audio-time-stretch",
                "--sub-file=auto",
                "--subsdec-encoding=auto",
                "--sub-autodetect-file",
                "--sub-autodetect-fuzzy=2"
            );
            
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.TimeChanged += OnTimeChanged;
            _mediaPlayer.LengthChanged += OnLengthChanged;
            _mediaPlayer.EnableHardwareDecoding = false;
            
            // 监听播放器状态变化，用于建立字幕轨道映射
            _mediaPlayer.Playing += OnMediaPlayerPlaying;
            
            // 初始化音频播放器（用于双播放器模式）
            _audioPlayer = new MediaPlayer(_libVLC);
            _audioPlayer.EnableHardwareDecoding = false;
            
            // 设置VideoView的MediaPlayer
            videoView.MediaPlayer = _mediaPlayer;
            
            Logger.Debug("VLC 和 MediaPlayer 创建完成");
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        #endregion

        #region 播放模式切换

        /// <summary>
        /// 切换播放模式
        /// </summary>
        private void OnPlayModeChanged(PlayMode newMode)
        {
            Logger.Debug("切换播放模式: {Mode}", newMode);
            Logger.Debug("OriginalVideoPath={OriginalVideoPath}", OriginalVideoPath);
            Logger.Debug("MutedVideoPath={MutedVideoPath}", MutedVideoPath);
            Logger.Debug("MergedAudioPath={MergedAudioPath}", MergedAudioPath);
            Logger.Debug("FinalVideoPath={FinalVideoPath}", FinalVideoPath);
            
            StopPlayback();
            
            _isDualPlayerMode = false;
            
            switch (newMode)
            {
                case PlayMode.OriginalVideo:
                    Logger.Debug("执行: 加载原始视频");
                    LoadVideo(OriginalVideoPath, _currentSubtitlePath);
                    break;
                    
                case PlayMode.MutedVideoWithAudio:
                    Logger.Debug("执行: 加载双播放器模式");
                    LoadDualPlayer(MutedVideoPath, MergedAudioPath, _currentSubtitlePath);
                    break;
                    
                case PlayMode.FinalVideo:
                    Logger.Debug("执行: 加载最终视频");
                    LoadVideo(FinalVideoPath, _currentSubtitlePath);
                    break;
            }
            
            // 加载完成后自动播放
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Play();
                
                // 双播放器模式：同时播放音频播放器
                if (_isDualPlayerMode && _audioPlayer != null)
                {
                    _audioPlayer.Play();
                }
                
                UpdateUIState(true);
                Logger.Debug("切换模式后自动播放");
            }
        }

        /// <summary>
        /// 加载双播放器模式（静音视频+音频）
        /// </summary>
        private void LoadDualPlayer(string videoPath, string audioPath)
        {
            LoadDualPlayer(videoPath, audioPath, null);
        }

        /// <summary>
        /// 加载双播放器模式（静音视频+音频+字幕）
        /// </summary>
        private void LoadDualPlayer(string videoPath, string audioPath, string? subtitlePath)
        {
            Logger.Debug("加载双播放器模式 - 视频: {VideoPath}, 音频: {AudioPath}, 字幕: {SubtitlePath}", videoPath, audioPath, subtitlePath ?? "无");
            
            try
            {
                if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
                {
                    Logger.Warning("视频文件不存在: {VideoPath}", videoPath);
                    return;
                }
                
                if (string.IsNullOrEmpty(audioPath) || !File.Exists(audioPath))
                {
                    Logger.Warning("音频文件不存在: {AudioPath}", audioPath);
                    return;
                }
                
                // 加载视频（静音）
                if (_media != null)
                {
                    _media.Dispose();
                }
                
                // 创建 Media 对象
                _media = new Media(_libVLC, videoPath, FromType.FromPath);
                
                // 一次性添加所有字幕
                _subtitleIndexToSpuId.Clear();
                for (int i = 0; i < _subtitleFiles.Count; i++)
                {
                    var subPath = _subtitleFiles[i].FilePath;
                    if (!string.IsNullOrEmpty(subPath) && File.Exists(subPath))
                    {
                        _media.AddSlave(MediaSlaveType.Subtitle, (uint)i, subPath);
                        Logger.Debug("添加字幕 {Index}: {SubPath}", i, subPath);
                    }
                }
                
                _mediaPlayer.Media = _media;
                
                // 加载音频
                if (_audioMedia != null)
                {
                    _audioMedia.Dispose();
                }
                _audioMedia = new Media(_libVLC, audioPath, FromType.FromPath);
                _audioPlayer.Media = _audioMedia;
                
                // 如果提供了字幕路径，设置当前字幕
                if (!string.IsNullOrEmpty(subtitlePath) && File.Exists(subtitlePath))
                {
                    _currentSubtitlePath = subtitlePath;
                    Logger.Debug("设置当前字幕: {SubtitlePath}", subtitlePath);
                }
                else
                {
                    _currentSubtitlePath = null;
                }
                
                _isDualPlayerMode = true;
                UpdateUIState(false);
                
                Logger.Debug("双播放器模式加载完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载双播放器模式失败");
                MessageBox.Show($"加载双播放器模式失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 事件处理

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_mediaPlayer != null && !_isDragging)
            {
                UpdateTimeDisplay();
            }
        }

        private void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            if (!_isDragging)
            {
                Dispatcher.Invoke(() =>
                {
                    progressSlider.Value = e.Time;
                    UpdateTimeDisplay();
                });
            }
        }

        private void OnLengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                progressSlider.Maximum = e.Length;
                totalTimeText.Text = FormatTime(e.Length);
            });
        }

        private void OnMediaPlayerPlaying(object sender, EventArgs e)
        {
            try
            {
                Logger.Debug("播放器开始播放，建立字幕轨道映射");
                
                // 获取所有可用的字幕轨道
                var spuDescriptions = _mediaPlayer?.SpuDescription;
                if (spuDescriptions == null || spuDescriptions.Length == 0)
                {
                    Logger.Debug("没有可用的字幕轨道");
                    return;
                }

                // 建立字幕索引到 SPU ID 的映射
                _subtitleIndexToSpuId.Clear();
                for (int i = 0; i < _subtitleFiles.Count && i < spuDescriptions.Length; i++)
                {
                    _subtitleIndexToSpuId[i] = spuDescriptions[i].Id;
                    Logger.Debug("字幕索引 {Index} -> SPU ID {SpuId}", i, spuDescriptions[i].Id);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "建立字幕轨道映射失败");
            }
        }

        #endregion

        #region 播放控制

        public void LoadVideo(string filename)
        {
            LoadVideo(filename, null);
        }

        public void LoadVideo(string filename, string? subtitlePath)
        {
            Logger.Debug("加载视频: {Filename}, 字幕: {SubtitlePath}", filename, subtitlePath ?? "无");
            
            try
            {
                if (_media != null)
                {
                    _media.Dispose();
                }
                
                // 创建 Media 对象
                _media = new Media(_libVLC, filename, FromType.FromPath);
                
                // 一次性添加所有字幕
                _subtitleIndexToSpuId.Clear();
                for (int i = 0; i < _subtitleFiles.Count; i++)
                {
                    var subPath = _subtitleFiles[i].FilePath;
                    if (!string.IsNullOrEmpty(subPath) && File.Exists(subPath))
                    {
                        _media.AddSlave(MediaSlaveType.Subtitle, (uint)i, subPath);
                        Logger.Debug("添加字幕 {Index}: {SubPath}", i, subPath);
                    }
                }
                
                _mediaPlayer.Media = _media;
                
                // 如果提供了字幕路径，设置当前字幕
                if (!string.IsNullOrEmpty(subtitlePath) && File.Exists(subtitlePath))
                {
                    _currentSubtitlePath = subtitlePath;
                    Logger.Debug("设置当前字幕: {SubtitlePath}", subtitlePath);
                }
                else
                {
                    _currentSubtitlePath = null;
                }
                
                UpdateUIState(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载视频失败");
                MessageBox.Show($"加载视频失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadSubtitle(string fileName)
        {
            Logger.Debug("加载字幕: {FileName}", fileName);
            
            try
            {
                if (_media != null && File.Exists(fileName))
                {
                    _media.AddOption($":sub-file={fileName}");
                    
                    // 更新字幕下拉框
                    subtitleComboBox.Items.Add(Path.GetFileName(fileName));
                    subtitleComboBox.SelectedIndex = subtitleComboBox.Items.Count - 1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载字幕失败");
                MessageBox.Show($"加载字幕失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 停止视频播放
        /// </summary>
        public void StopPlayback()
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    Logger.Debug("停止播放");
                    _mediaPlayer.Stop();
                    
                    // 双播放器模式：同时停止音频播放器
                    if (_isDualPlayerMode && _audioPlayer != null)
                    {
                        _audioPlayer.Stop();
                    }
                    
                    UpdateUIState(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "停止播放异常");
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Play();
                
                // 双播放器模式：同时播放音频播放器
                if (_isDualPlayerMode && _audioPlayer != null)
                {
                    _audioPlayer.Play();
                }
                
                UpdateUIState(true);
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Pause();
                
                // 双播放器模式：同时暂停音频播放器
                if (_isDualPlayerMode && _audioPlayer != null)
                {
                    _audioPlayer.Pause();
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                
                // 双播放器模式：同时停止音频播放器
                if (_isDualPlayerMode && _audioPlayer != null)
                {
                    _audioPlayer.Stop();
                }
                
                UpdateUIState(false);
            }
        }

        #endregion

        #region 进度控制

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDragging && _mediaPlayer != null)
            {
                _mediaPlayer.Time = (long)e.NewValue;
                
                // 双播放器模式：同时设置音频播放器时间
                if (_isDualPlayerMode && _audioPlayer != null)
                {
                    _audioPlayer.Time = (long)e.NewValue;
                }
                
                UpdateTimeDisplay();
            }
        }

        private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        #endregion

        #region 音量控制

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = (int)(e.NewValue * 100);
                volumeText.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        #endregion

        #region 字幕控制

        /// <summary>
        /// 更新字幕列表
        /// </summary>
        public void UpdateSubtitleList(List<SubtitleFileInfo> subtitleFiles)
        {
            try
            {
                Logger.Debug("更新字幕列表，共 {Count} 个字幕", subtitleFiles.Count);

                _subtitleFiles = subtitleFiles;
                _currentSubtitleIndex = -1;
                _currentSubtitlePath = null;

                subtitleComboBox.Items.Clear();

                foreach (var subtitle in subtitleFiles)
                {
                    subtitleComboBox.Items.Add(subtitle.DisplayName);
                }

                if (subtitleFiles.Count > 0)
                {
                    // 先设置索引，避免触发 SelectionChanged
                    _currentSubtitleIndex = 0;
                    _currentSubtitlePath = subtitleFiles[0].FilePath;
                    subtitleComboBox.SelectedIndex = 0;
                    Logger.Debug("默认选择第一个字幕: {DisplayName}", subtitleFiles[0].DisplayName);

                    // 切换到第一个字幕
                    SwitchSubtitle(0);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新字幕列表失败");
            }
        }

        /// <summary>
        /// 切换字幕（使用 SetSpu 动态切换）
        /// </summary>
        private void SwitchSubtitle(int subtitleIndex)
        {
            try
            {
                Logger.Debug("切换字幕到索引: {SubtitleIndex}", subtitleIndex);

                if (_mediaPlayer == null)
                {
                    Logger.Warning("无法切换字幕：MediaPlayer 为空");
                    return;
                }

                var subtitlePath = _subtitleFiles[subtitleIndex].FilePath;
                
                // 如果字幕路径相同，直接返回
                if (_currentSubtitlePath == subtitlePath)
                {
                    Logger.Debug("字幕路径相同，无需切换");
                    return;
                }

                Logger.Debug("使用 SetSpu 切换字幕: {SubtitlePath}", subtitlePath);

                // 使用 SetSpu 切换到指定的字幕轨道
                if (_subtitleIndexToSpuId.ContainsKey(subtitleIndex))
                {
                    int spuId = _subtitleIndexToSpuId[subtitleIndex];
                    bool success = _mediaPlayer.SetSpu(spuId);
                    
                    if (success)
                    {
                        Logger.Debug("SetSpu 成功: SPU ID = {SpuId}", spuId);
                        _currentSubtitleIndex = subtitleIndex;
                        _currentSubtitlePath = subtitlePath;
                    }
                    else
                    {
                        Logger.Warning("SetSpu 失败: SPU ID = {SpuId}", spuId);
                    }
                }
                else
                {
                    Logger.Warning("找不到字幕索引 {SubtitleIndex} 对应的 SPU ID", subtitleIndex);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "切换字幕失败");
                MessageBox.Show($"切换字幕失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 动态切换字幕（重新加载 Media 对象）
        /// </summary>
        private void ReloadVideoWithSubtitle(string subtitlePath)
        {
            try
            {
                Logger.Debug("切换字幕: {SubtitlePath}", subtitlePath);

                if (_mediaPlayer == null || _media == null)
                {
                    Logger.Warning("MediaPlayer 或 Media 为空，无法切换字幕");
                    return;
                }

                // 保存当前播放位置和播放状态
                long currentTime = _mediaPlayer.Time;
                bool isPlaying = _mediaPlayer.IsPlaying;
                Logger.Debug("当前播放位置: {CurrentTime}ms, 播放状态: {IsPlaying}", currentTime, isPlaying);

                // 停止播放
                _mediaPlayer.Stop();
                if (_isDualPlayerMode && _audioPlayer != null)
                {
                    _audioPlayer.Stop();
                }

                // 根据当前播放模式重新加载视频（带新字幕）
                switch (CurrentPlayMode)
                {
                    case PlayMode.OriginalVideo:
                        LoadVideo(OriginalVideoPath, subtitlePath);
                        break;
                    case PlayMode.MutedVideoWithAudio:
                        LoadDualPlayer(MutedVideoPath, MergedAudioPath, subtitlePath);
                        break;
                    case PlayMode.FinalVideo:
                        LoadVideo(FinalVideoPath, subtitlePath);
                        break;
                }

                // 重新播放
                _mediaPlayer.Play();
                if (_isDualPlayerMode && _audioPlayer != null)
                {
                    _audioPlayer.Play();
                }

                // 等待播放器准备就绪后恢复播放位置
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (currentTime > 0 && _mediaPlayer != null)
                    {
                        _mediaPlayer.Time = currentTime;
                        if (_isDualPlayerMode && _audioPlayer != null)
                        {
                            _audioPlayer.Time = currentTime;
                        }
                        Logger.Debug("播放位置已恢复: {CurrentTime}ms", currentTime);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                _currentSubtitlePath = subtitlePath;
                Logger.Debug("字幕切换完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "切换字幕失败");
                MessageBox.Show($"切换字幕失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 字幕选择变更事件
        /// </summary>
        private void SubtitleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (subtitleComboBox.SelectedIndex < 0 || subtitleComboBox.SelectedIndex >= _subtitleFiles.Count)
                {
                    return;
                }

                var newIndex = subtitleComboBox.SelectedIndex;
                
                if (newIndex == _currentSubtitleIndex)
                {
                    return;
                }

                var selectedSubtitle = _subtitleFiles[newIndex];
                Logger.Debug("选择字幕: {DisplayName}, 路径: {FilePath}", selectedSubtitle.DisplayName, selectedSubtitle.FilePath);

                if (!File.Exists(selectedSubtitle.FilePath))
                {
                    Logger.Warning("字幕文件不存在: {FilePath}", selectedSubtitle.FilePath);
                    MessageBox.Show($"字幕文件不存在: {selectedSubtitle.FilePath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 使用预加载的 Media 切换字幕
                SwitchSubtitle(newIndex);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "字幕选择变更失败");
            }
        }

        #endregion

        #region 播放模式控制

        /// <summary>
        /// 播放模式按钮点击事件
        /// </summary>
        private void PlayModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                var modeString = button.Tag.ToString();
                if (Enum.TryParse<PlayMode>(modeString, out var mode))
                {
                    Logger.Debug("用户选择播放模式: {Mode}", mode);
                    
                    // 检查播放模式是否可用
                    var unavailableReasons = PlayModeUnavailableReasons ?? new Dictionary<PlayMode, string>();
                    var availableModes = new HashSet<PlayMode>(PlayModesAvailable ?? new PlayMode[0]);
                    
                    if (!availableModes.Contains(mode))
                    {
                        // 播放模式不可用，显示对话框说明原因
                        if (unavailableReasons.TryGetValue(mode, out var reason))
                        {
                            Logger.Debug("播放模式不可用: {Mode}, 原因: {Reason}", mode, reason);
                            MessageBox.Show($"无法播放此模式：\n\n{reason}", "提示", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            Logger.Debug("播放模式不可用: {Mode}, 无原因信息", mode);
                            MessageBox.Show($"无法播放此模式：{mode}", "提示", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        return;
                    }
                    
                    // 即使模式相同，也重新加载视频
                    if (CurrentPlayMode == mode)
                    {
                        Logger.Debug("模式相同，重新加载: {Mode}", mode);
                        OnPlayModeChanged(mode);
                    }
                    else
                    {
                        CurrentPlayMode = mode;
                    }
                    
                    UpdatePlayModeButtons();
                }
            }
        }

        /// <summary>
        /// 更新播放模式按钮状态
        /// </summary>
        private void UpdatePlayModeButtons()
        {
            Logger.Debug("UpdatePlayModeButtons 被调用");

            if (PlayModesAvailable == null || PlayModesAvailable.Length == 0)
            {
                Logger.Debug("PlayModesAvailable 为空");
                return;
            }

            Logger.Debug("PlayModesAvailable: {PlayModesAvailable}", string.Join(", ", PlayModesAvailable));

            var availableModes = new HashSet<PlayMode>(PlayModesAvailable);
            var unavailableReasons = PlayModeUnavailableReasons ?? new Dictionary<PlayMode, string>();

            // 更新原始视频按钮
            UpdatePlayModeButton(originalVideoButton, PlayMode.OriginalVideo, availableModes, unavailableReasons);

            // 更新静音视频+音频按钮
            UpdatePlayModeButton(mutedVideoWithAudioButton, PlayMode.MutedVideoWithAudio, availableModes, unavailableReasons);

            // 更新最终视频按钮
            UpdatePlayModeButton(finalVideoButton, PlayMode.FinalVideo, availableModes, unavailableReasons);
        }

        /// <summary>
        /// 更新单个播放模式按钮的状态
        /// </summary>
        private void UpdatePlayModeButton(Button button, PlayMode mode, HashSet<PlayMode> availableModes, Dictionary<PlayMode, string> unavailableReasons)
        {
            Logger.Debug("UpdatePlayModeButton: mode={Mode}, button={ButtonExists}", mode, button != null ? "存在" : "null");

            if (button == null)
            {
                return;
            }

            var isAvailable = availableModes.Contains(mode);
            button.IsEnabled = true;

            Logger.Debug("按钮状态: mode={Mode}, isAvailable={IsAvailable}", mode, isAvailable);

            // 高亮当前选中的播放模式（使用不同的背景颜色表示选中状态）
            if (CurrentPlayMode == mode)
            {
                button.Background = System.Windows.Media.Brushes.DarkBlue;
                button.FontWeight = System.Windows.FontWeights.Bold;
            }
            else
            {
                button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243));
                button.FontWeight = System.Windows.FontWeights.Normal;
            }
        }

        #endregion

        #region 辅助方法

        private void UpdateTimeDisplay()
        {
            if (_mediaPlayer != null)
            {
                currentTimeText.Text = FormatTime(_mediaPlayer.Time);
            }
        }

        private string FormatTime(long milliseconds)
        {
            var time = TimeSpan.FromMilliseconds(milliseconds);
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        private void UpdateUIState(bool isPlaying)
        {
            playButton.IsEnabled = !isPlaying;
            pauseButton.IsEnabled = isPlaying;
            stopButton.IsEnabled = isPlaying;
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
                // 停止定时器
                _timer?.Stop();
                _timer = null;

                // 停止并释放音频播放器（双播放器模式）
                if (_audioPlayer != null)
                {
                    Logger.Debug("停止音频播放器");
                    try
                    {
                        _audioPlayer.Stop();
                        System.Threading.Thread.Sleep(200);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "停止音频播放器时异常");
                    }
                }

                // 释放音频Media
                if (_audioMedia != null)
                {
                    Logger.Debug("释放音频 Media");
                    _audioMedia.Dispose();
                    _audioMedia = null;
                }

                // 释放音频播放器
                if (_audioPlayer != null)
                {
                    Logger.Debug("释放音频播放器");
                    _audioPlayer.Dispose();
                    _audioPlayer = null;
                }

                // 停止并释放MediaPlayer
                if (_mediaPlayer != null)
                {
                    Logger.Debug("MediaPlayer 状态: {State}, 是否播放中: {IsPlaying}", _mediaPlayer.State, _mediaPlayer.IsPlaying);
                    Logger.Debug("停止播放");
                    
                    // 取消订阅事件
                    _mediaPlayer.Playing -= OnMediaPlayerPlaying;
                    
                    try
                    {
                        // 先停止播放
                        _mediaPlayer.Stop();
                        
                        // 等待一段时间确保停止完成
                        System.Threading.Thread.Sleep(200);
                        
                        Logger.Debug("播放已停止");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "停止播放时异常");
                    }
                }

                // 释放Media
                if (_media != null)
                {
                    Logger.Debug("释放 Media");
                    _media.Dispose();
                    _media = null;
                }

                // 释放MediaPlayer
                if (_mediaPlayer != null)
                {
                    Logger.Debug("释放 MediaPlayer");
                    _mediaPlayer.Dispose();
                    _mediaPlayer = null;
                }

                // 释放LibVLC
                if (_libVLC != null)
                {
                    Logger.Debug("释放 LibVLC");
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

    #region 字幕信息类

    /// <summary>
    /// 字幕文件信息
    /// </summary>
    public class SubtitleFileInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    #endregion
}