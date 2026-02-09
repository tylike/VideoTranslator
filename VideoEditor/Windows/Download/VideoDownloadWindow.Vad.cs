using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using VideoTranslator.Models;
using VT.Module;
using VideoEditor.Models;
using VideoEditor.Services;

namespace VideoEditor.Windows;

public partial class VideoDownloadWindow
{

    private VadDetectionResult? _vadResult;
    private readonly ObservableCollection<VadSegmentDisplay> _vadSegments = new();
    private readonly ObservableCollection<SplitScheme> _splitSchemes = new();
    private SplitScheme? _selectedSplitScheme;
    private async void ExecuteVadButton_Click(object sender, RoutedEventArgs e)
    {
        var mediaPath = !string.IsNullOrEmpty(_downloadedVideoPath) ? _downloadedVideoPath : _downloadedAudioPath;

        if (string.IsNullOrEmpty(mediaPath))
        {
            MessageBox.Show("请先下载视频或音频", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _logger.Information("用户点击执行VAD检测按钮");

        try
        {
            var threshold = decimal.TryParse(VadThresholdTextBox.Text, out var t) ? t : 0.5m;
            var minSpeechDuration = int.TryParse(MinSpeechDurationTextBox.Text, out var msd) ? msd : 250;
            var minSilenceDuration = int.TryParse(MinSilenceDurationTextBox.Text, out var msi) ? msi : 100;

            ShowLoading(true);
            LoadingTextBlock.Text = "正在执行VAD检测...";

            var vadWorkflowService = ServiceHelper.GetService<VideoTranslator.Services.VideoVadWorkflowService>();
            var result = await vadWorkflowService.ExecuteFullVadWorkflowAsync(
                mediaPath,
                vadThreshold: threshold,
                minSpeechDurationMs: minSpeechDuration,
                minSilenceDurationMs: minSilenceDuration,
                splitVideo: false);

            _logger.Information("VAD检测完成: 语音段={SpeechCount}, 静音段={SilenceCount}",
                result.VadResult?.SpeechSegmentCount, result.VadResult?.SilenceSegmentCount);

            ShowLoading(false);

            if (!result.Success)
            {
                MessageBox.Show($"VAD检测失败: {result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var vadResult = result.VadResult;
            if (vadResult == null)
            {
                MessageBox.Show("VAD检测结果为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _vadResult = vadResult;

            #region 显示VAD检测结果

            DisplayVadResult(vadResult);

            #endregion

            #region 生成分割建议

            GenerateSplitSuggestions(vadResult);

            #endregion

            MessageBox.Show("VAD检测完成！请查看检测结果和分割建议。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "VAD检测失败");
            ShowLoading(false);
            MessageBox.Show($"VAD检测失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    /// <summary>
    /// 暂时先禁用自动vad检测的功能
    /// </summary>
    /// <param name="mediaPath"></param>
    /// <returns></returns>
    private async Task<bool> ExecuteVad(string mediaPath)
    {
        
        return true;
        ShowLoading(true);
        LoadingTextBlock.Text = "正在检测音频时长...";

        try
        {
            var vadWorkflowService = ServiceHelper.GetService<VideoTranslator.Services.VideoVadWorkflowService>();
            var audioDuration = await vadWorkflowService.GetAudioDurationAsync(mediaPath);
            _logger.Information("音频时长: {Duration}秒", audioDuration);

            if (audioDuration > 300)
            {
                _logger.Information("音频时长大于5分钟，自动切换到VAD检测标签页并执行VAD检测");
                MainTabControl.SelectedIndex = 1;
                ShowLoading(true);
                LoadingTextBlock.Text = "正在执行VAD检测...";

                var threshold = decimal.TryParse(VadThresholdTextBox.Text, out var t) ? t : 0.5m;
                var minSpeechDuration = int.TryParse(MinSpeechDurationTextBox.Text, out var msd) ? msd : 250;
                var minSilenceDuration = int.TryParse(MinSilenceDurationTextBox.Text, out var msi) ? msi : 100;

                var result = await vadWorkflowService.ExecuteFullVadWorkflowAsync(
                    mediaPath,
                    vadThreshold: threshold,
                    minSpeechDurationMs: minSpeechDuration,
                    minSilenceDurationMs: minSilenceDuration,
                    splitVideo: false);

                _logger.Information("VAD检测完成: 语音段={SpeechCount}, 静音段={SilenceCount}",
                    result.VadResult?.SpeechSegmentCount, result.VadResult?.SilenceSegmentCount);

                if (!result.Success)
                {
                    ShowLoading(false);
                    MessageBox.Show($"VAD检测失败: {result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                var vadResult = result.VadResult;
                if (vadResult == null)
                {
                    ShowLoading(false);
                    MessageBox.Show("VAD检测结果为空", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                _vadResult = vadResult;

                #region 显示VAD检测结果

                DisplayVadResult(vadResult);

                #endregion

                #region 生成分割建议

                GenerateSplitSuggestions(vadResult);

                #endregion

                ShowLoading(false);
                MessageBox.Show($"下载完成！音频时长为{audioDuration:F2}秒（约{audioDuration / 60:F1}分钟），已超过5分钟。\n已自动执行VAD检测，请查看检测结果和分割建议。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "检测音频时长失败，跳过自动VAD检测");
        }

        return true;
    }

    private async void ExecuteSplitButton_Click(object sender, RoutedEventArgs e)
    {
        var mediaPath = !string.IsNullOrEmpty(_downloadedVideoPath) ? _downloadedVideoPath : _downloadedAudioPath;

        if (_vadResult == null || string.IsNullOrEmpty(mediaPath) || _selectedSplitScheme == null)
        {
            MessageBox.Show("请先执行VAD检测并选择分割方案", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _logger.Information("用户点击执行分割按钮，选择方案: {Index}, 分割点数: {Count}",
            _selectedSplitScheme.Index, _selectedSplitScheme.SplitPoints.Count);

        try
        {
            ShowLoading(true);
            var isVideo = !string.IsNullOrEmpty(_downloadedVideoPath);
            LoadingTextBlock.Text = isVideo ? "正在分割视频..." : "正在分割音频...";

            var vadWorkflowService = ServiceHelper.GetService<VideoTranslator.Services.VideoVadWorkflowService>();
            var downloadFolder = Path.GetDirectoryName(mediaPath) ?? "";

            var splitFiles = vadWorkflowService.SplitVideoByPoints(
                mediaPath,
                _selectedSplitScheme.SplitPoints,
                downloadFolder);

            _logger.Information("媒体分割完成: 分割文件数={Count}", splitFiles.Count);

            var vadInfoPath = Path.Combine(downloadFolder, "vad_info.json");
            vadWorkflowService.SaveVadInfo(vadInfoPath, _vadResult, splitFiles);
            _logger.Information("VAD信息已保存: {VadInfoPath}", vadInfoPath);

            ShowLoading(false);
            var mediaType = isVideo ? "视频" : "音频";
            MessageBox.Show($"{mediaType}分割完成，共生成 {splitFiles.Count} 个片段\n保存位置: {downloadFolder}", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "媒体分割失败");
            ShowLoading(false);
            MessageBox.Show($"媒体分割失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DisplayVadResult(VadDetectionResult vadResult)
    {
        VadResultGroupBox.Visibility = Visibility.Visible;

        VadDurationText.Text = TimeFormatter.FormatDuration(vadResult.AudioDuration);
        VadSpeechCountText.Text = vadResult.SpeechSegmentCount.ToString();
        VadSilenceCountText.Text = vadResult.SilenceSegmentCount.ToString();
        VadTotalSpeechText.Text = TimeFormatter.FormatDuration(vadResult.TotalSpeechDuration);
        VadTotalSilenceText.Text = TimeFormatter.FormatDuration(vadResult.TotalSilenceDuration);

        _vadSegments.Clear();
        foreach (var segment in vadResult.Segments)
        {
            _vadSegments.Add(new VadSegmentDisplay
            {
                Index = segment.Index,
                Start = segment.Start,
                End = segment.End,
                Duration = segment.Duration,
                Type = segment.IsSpeech ? "语音" : "静音"
            });
        }

        VadSegmentsDataGrid.ItemsSource = _vadSegments;
    }

    private void GenerateSplitSuggestions(VadDetectionResult vadResult)
    {
        SplitSuggestionGroupBox.Visibility = Visibility.Visible;
        _splitSchemes.Clear();

        var splitter = new AudioSplitter();
        var schemes = splitter.GenerateSplitSchemes(vadResult);

        foreach (var scheme in schemes)
        {
            _splitSchemes.Add(scheme);
        }

        SplitSuggestionDataGrid.ItemsSource = _splitSchemes;

        var optimalScheme = FindOptimalScheme(schemes, vadResult);
        if (optimalScheme != null)
        {
            SplitSuggestionDataGrid.SelectedItem = optimalScheme;
            _selectedSplitScheme = optimalScheme;
            SchemeSegmentsDataGrid.ItemsSource = optimalScheme.Segments;
            ExecuteSplitButton.IsEnabled = true;
        }
    }

    private static SplitScheme? FindOptimalScheme(List<SplitScheme> schemes, VadDetectionResult vadResult)
    {
        const decimal MAX_SEGMENT_DURATION = 300m;

        var validSchemes = schemes
            .Where(s => s.SplitPoints.Count > 0)
            .Select(s => new
            {
                Scheme = s,
                MaxSegment = GetMaxSegmentDuration(s, vadResult)
            })
            .Where(x => x.MaxSegment <= MAX_SEGMENT_DURATION)
            .OrderByDescending(x => x.Scheme.SplitPoints.Count)
            .ToList();

        return validSchemes.Any() ? validSchemes.First().Scheme : schemes.FirstOrDefault();
    }

    private static decimal GetMaxSegmentDuration(SplitScheme scheme, VadDetectionResult vadResult)
    {
        var currentTime = 0m;
        var maxDuration = 0m;

        foreach (var splitPoint in scheme.SplitPoints)
        {
            var segmentDuration = splitPoint - currentTime;
            if (segmentDuration > maxDuration)
            {
                maxDuration = segmentDuration;
            }
            currentTime = splitPoint;
        }

        var lastSegmentDuration = vadResult.AudioDuration - currentTime;
        if (lastSegmentDuration > maxDuration)
        {
            maxDuration = lastSegmentDuration;
        }

        return maxDuration;
    }

    private void SplitSuggestionDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SplitSuggestionDataGrid.SelectedItem is SplitScheme scheme)
        {
            _selectedSplitScheme = scheme;
            SchemeSegmentsDataGrid.ItemsSource = scheme.Segments;
            ExecuteSplitButton.IsEnabled = true;
        }
        else
        {
            _selectedSplitScheme = null;
            SchemeSegmentsDataGrid.ItemsSource = null;
            ExecuteSplitButton.IsEnabled = false;
        }
    }

}
