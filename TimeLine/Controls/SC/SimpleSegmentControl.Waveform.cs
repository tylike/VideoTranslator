using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Serilog;
using TimeLine.Services;
using VT.Module.BusinessObjects;
using VideoTranslator.Interfaces;

namespace TimeLine.Controls;

public partial class SimpleSegmentControl
{
    #region 波形字段

    private bool _isImageDirty = true;
    private WriteableBitmap? _cachedWaveformImage;
    private Image? _waveformImage;
    private DispatcherTimer? _debounceTimer;
    private const int DebounceDelayMs = 100;
    private System.Threading.Tasks.Task? _backgroundTask;
    private readonly object _lockObj = new object();
    private const int WaveformImageHeight = 50;

    #endregion

    #region 波形初始化

    private void InitializeWaveformEvents()
    {
        Loaded += OnControlLoadedForWaveform;
        SizeChanged += OnControlSizeChangedForWaveform;
        InitializeDebounceTimer();
    }

    private void InitializeDebounceTimer()
    {
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(DebounceDelayMs)
        };
        _debounceTimer.Tick += (s, e) =>
        {
            _debounceTimer?.Stop();
            UpdateWaveformImage();
        };
    }

    #endregion

    #region 波形UI更新

    private void OnControlLoadedForWaveform(object sender, RoutedEventArgs e)
    {
        _logger.Debug("[SimpleSegmentControl] 波形控件已加载: ClipIndex={ClipIndex}", _clip?.Index);

        CacheWaveformImage();

        if (_clip is IWaveform waveform && waveform.ShowWaveform && waveform.WaveformData?.Count > 0)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateWaveformImage();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnControlSizeChangedForWaveform(object sender, SizeChangedEventArgs e)
    {
        _logger.Debug("[SimpleSegmentControl] 波形控件尺寸变化: ClipIndex={ClipIndex}, NewWidth={Width:F2}, NewHeight={Height:F2}",
            _clip?.Index, e.NewSize.Width, e.NewSize.Height);

        if (e.NewSize.Width > 0 && e.NewSize.Height > 0 && _clip is IWaveform waveform && waveform.ShowWaveform && waveform.WaveformData?.Count > 0)
        {
            _isImageDirty = true;
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }
    }

    private void UpdateWaveformImage()
    {
        if (!Dispatcher.CheckAccess())
        {
            if (!_updatePending)
            {
                Dispatcher.BeginInvoke(new Action(UpdateWaveformImage), DispatcherPriority.Render);
            }
            return;
        }

        UpdateWaveformImageInternal();
    }

    private void UpdateWaveformImageInternal()
    {
        if (_waveformImage == null || _clip == null)
        {
            return;
        }

        if (_clip is not IWaveform waveform)
        {
            _waveformImage.Visibility = Visibility.Collapsed;
            UpdateText();
            return;
        }

        if (!waveform.ShowWaveform || waveform.WaveformData == null || waveform.WaveformData.Count == 0)
        {
            _waveformImage.Visibility = Visibility.Collapsed;
            UpdateText();
            return;
        }

        _waveformImage.Visibility = Visibility.Visible;

        var width = ActualWidth > 0 ? ActualWidth : Width;

        if (!_isImageDirty && _cachedWaveformImage != null && Math.Abs(_cachedWaveformImage.PixelWidth - width) < 1)
        {
            return;
        }

        System.Threading.Tasks.Task? currentTask = null;
        
        lock (_lockObj)
        {
            if (_backgroundTask != null && !_backgroundTask.IsCompleted)
            {
                return;
            }

            var waveformDataCopy = new List<double>(waveform.WaveformData);
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(RowColor);
            var imageWidth = Math.Max(1, (int)Math.Ceiling(width));

            _backgroundTask = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var bitmap = GenerateWaveformBitmap(waveformDataCopy, imageWidth, WaveformImageHeight, color);

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        lock (_lockObj)
                        {
                            _cachedWaveformImage = bitmap;
                            _isImageDirty = false;

                            if (_waveformImage != null)
                            {
                                _waveformImage.Source = _cachedWaveformImage;
                            }
                        }
                    }), DispatcherPriority.Render);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[SimpleSegmentControl] 后台线程生成波形图片失败: ClipIndex={ClipIndex}", _clip?.Index);
                }
            });
            
            currentTask = _backgroundTask;
        }
        
        currentTask?.ContinueWith(t =>
        {
            lock (_lockObj)
            {
                if (_backgroundTask == t)
                {
                    _backgroundTask = null;
                }
            }
        }, System.Threading.CancellationToken.None, System.Threading.Tasks.TaskContinuationOptions.None, System.Threading.Tasks.TaskScheduler.Default);
    }

    private WriteableBitmap GenerateWaveformBitmap(IList<double> waveformData, int width, int height, Color color)
    {
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        var stride = width * 4;
        var pixels = new byte[height * stride];

        var dataCount = waveformData.Count;
        if (dataCount == 0)
        {
            return bitmap;
        }

        var step = (double)width / dataCount;
        var r = color.R;
        var g = color.G;
        var b = color.B;
        var a = color.A;

        for (int x = 0; x < width; x++)
        {
            var dataIndex = (int)(x / step);
            if (dataIndex >= dataCount)
            {
                dataIndex = dataCount - 1;
            }

            var amplitude = waveformData[dataIndex];
            var centerY = height / 2;
            var halfHeight = (int)(amplitude * (height / 2));

            for (int y = 0; y < height; y++)
            {
                var pixelIndex = y * stride + x * 4;
                
                if (y >= centerY - halfHeight && y <= centerY + halfHeight)
                {
                    pixels[pixelIndex] = b;
                    pixels[pixelIndex + 1] = g;
                    pixels[pixelIndex + 2] = r;
                    pixels[pixelIndex + 3] = a;
                }
                else
                {
                    pixels[pixelIndex] = 0;
                    pixels[pixelIndex + 1] = 0;
                    pixels[pixelIndex + 2] = 0;
                    pixels[pixelIndex + 3] = 0;
                }
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        bitmap.Freeze();
        return bitmap;
    }

    private void UpdateWaveformColor()
    {
        if (!Dispatcher.CheckAccess())
        {
            if (!_updatePending)
            {
                Dispatcher.BeginInvoke(new Action(UpdateWaveformColor), DispatcherPriority.Render);
            }
            return;
        }

        if (_waveformImage == null)
        {
            return;
        }

        _isImageDirty = true;
        UpdateWaveformImage();
    }

    private void CacheWaveformImage()
    {
        if (_waveformImage != null)
        {
            return;
        }

        _waveformImage = this.FindName("波形图片") as Image;
    }

    #endregion
}
