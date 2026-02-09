using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VideoTranslator.Config;
using Serilog;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace VideoTranslator.Services;

public static class YtDlpService
{
    #region 私有字段

    private static readonly YoutubeDL _youtubeDL;
    private static readonly string _ytDlpPath;
    private static readonly string _ffmpegPath;

    #endregion

    #region 构造函数

    static YtDlpService()
    {
        var settings = ConfigurationService.Configuration;
        _ytDlpPath = settings.VideoTranslator.Paths.YtDlpPath ?? "yt-dlp";
        _ffmpegPath = settings.VideoTranslator.Paths.FfmpegPath ?? "ffmpeg";

        _youtubeDL = new YoutubeDL
        {
            YoutubeDLPath = _ytDlpPath,
            FFmpegPath = _ffmpegPath,
            OutputFolder = Environment.CurrentDirectory,
            OutputFileTemplate = "%(title)s [%(id)s].%(ext)s",
            RestrictFilenames = false,
            OverwriteFiles = true,
            IgnoreDownloadErrors = true
        };
    }

    #endregion

    #region 公共方法 - 获取视频信息

    public static async Task<VideoData> GetVideoInfoAsync(string url, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report("正在获取视频信息...");

            var result = await _youtubeDL.RunVideoDataFetch(url, cancellationToken, flat: true, fetchComments: false);
            var rst = result.Data.Item1;

            if (!result.Success || rst == null)
            {
                throw new InvalidOperationException($"获取视频信息失败: {string.Join(", ", result.ErrorOutput)}");
            }

            var info = result.Data.Item1;

            progress?.Report($"获取成功: {info.Title}");

            return info;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取视频信息失败: {Url}", url);
            throw;
        }
    }

    #endregion

    #region 公共方法 - 下载视频

    public static async Task<string> DownloadVideoAsync(string url, string outputPath, string? formatId = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report("开始下载视频...");

            var outputDir = Path.GetDirectoryName(outputPath);
            var outputFileName = Path.GetFileNameWithoutExtension(outputPath);
            var outputExt = Path.GetExtension(outputPath).TrimStart('.');

            if (!string.IsNullOrEmpty(outputDir))
            {
                _youtubeDL.OutputFolder = outputDir;
            }

            var format = string.IsNullOrEmpty(formatId) ? "bestvideo+bestaudio/best" : formatId;

            var progressReporter = new Progress<YoutubeDLSharp.DownloadProgress>(p =>
            {
                if (p.State == YoutubeDLSharp.DownloadState.Downloading)
                {
                    progress?.Report($"下载中: {p.Progress * 100:F1}%");
                }
                else if (p.State == YoutubeDLSharp.DownloadState.PostProcessing)
                {
                    progress?.Report("正在处理...");
                }
                else if (p.State == YoutubeDLSharp.DownloadState.Success)
                {
                    progress?.Report("下载完成");
                }
            });

            var outputReporter = new Progress<string>(p => progress?.Report(p));

            var opts = new OptionSet
            {
                Format = format,
                MergeOutputFormat = DownloadMergeFormat.Mkv,
                RecodeVideo = VideoRecodeFormat.None,
                Output = Path.Combine(outputDir ?? Environment.CurrentDirectory, $"{outputFileName}.%(ext)s")
            };

            var result = await _youtubeDL.RunWithOptions(url, opts, cancellationToken, progress: progressReporter, output: outputReporter);

            if (!result.Success)
            {
                throw new InvalidOperationException($"下载视频失败: {string.Join(", ", result.ErrorOutput)}");
            }

            var downloadedFile = result.Data;
            if (!string.IsNullOrEmpty(downloadedFile) && File.Exists(downloadedFile))
            {
                if (!string.IsNullOrEmpty(outputPath) && !string.Equals(downloadedFile, outputPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(downloadedFile, outputPath, true);
                }
                return outputPath;
            }

            return downloadedFile ?? outputPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "下载视频失败: {Url}", url);
            throw;
        }
    }

    #endregion

    #region 公共方法 - 下载音频

    public static async Task<string> DownloadAudioAsync(string url, string outputPath, string? formatId = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report("开始下载音频...");

            var outputDir = Path.GetDirectoryName(outputPath);
            var outputFileName = Path.GetFileNameWithoutExtension(outputPath);
            var outputExt = Path.GetExtension(outputPath).TrimStart('.');

            if (!string.IsNullOrEmpty(outputDir))
            {
                _youtubeDL.OutputFolder = outputDir;
            }

            var format = string.IsNullOrEmpty(formatId) ? "bestaudio/best" : formatId;

            var progressReporter = new Progress<YoutubeDLSharp.DownloadProgress>(p =>
            {
                if (p.State == YoutubeDLSharp.DownloadState.Downloading)
                {
                    progress?.Report($"下载中: {p.Progress * 100:F1}%");
                }
                else if (p.State == YoutubeDLSharp.DownloadState.PostProcessing)
                {
                    progress?.Report("正在处理...");
                }
                else if (p.State == YoutubeDLSharp.DownloadState.Success)
                {
                    progress?.Report("下载完成");
                }
            });

            var outputReporter = new Progress<string>(p => progress?.Report(p));

            var opts = new OptionSet
            {
                Format = format,
                ExtractAudio = true,
                AudioFormat = AudioConversionFormat.Mp3,
                Output = Path.Combine(outputDir ?? Environment.CurrentDirectory, $"{outputFileName}.%(ext)s")
            };

            var result = await _youtubeDL.RunWithOptions(url, opts, cancellationToken, progress: progressReporter, output: outputReporter);

            if (!result.Success)
            {
                throw new InvalidOperationException($"下载音频失败: {string.Join(", ", result.ErrorOutput)}");
            }

            var downloadedFile = result.Data;
            if (!string.IsNullOrEmpty(downloadedFile) && File.Exists(downloadedFile))
            {
                if (!string.IsNullOrEmpty(outputPath) && !string.Equals(downloadedFile, outputPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Move(downloadedFile, outputPath, true);
                }
                return outputPath;
            }

            return downloadedFile ?? outputPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "下载音频失败: {Url}", url);
            throw;
        }
    }

    #endregion

    #region 公共方法 - 下载字幕

    public static async Task<string> DownloadSubtitleAsync(string url, string outputPath, string languageCode, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            progress?.Report($"开始下载字幕 ({languageCode})...");

            var outputDir = Path.GetDirectoryName(outputPath);
            var outputFileName = Path.GetFileNameWithoutExtension(outputPath);
            var outputExt = Path.GetExtension(outputPath).TrimStart('.');

            if (!string.IsNullOrEmpty(outputDir))
            {
                _youtubeDL.OutputFolder = outputDir;
            }

            var opts = new OptionSet
            {
                WriteSubs = true,
                SubLangs = languageCode,
                SubFormat = "srt",
                SkipDownload = true,
                Output = Path.Combine(outputDir ?? Environment.CurrentDirectory, $"{outputFileName}.%(ext)s")
            };

            var progressReporter = new Progress<YoutubeDLSharp.DownloadProgress>(p =>
            {
                if (p.State == YoutubeDLSharp.DownloadState.Downloading)
                {
                    progress?.Report($"下载中: {p.Progress * 100:F1}%");
                }
                else if (p.State == YoutubeDLSharp.DownloadState.Success)
                {
                    progress?.Report("下载完成");
                }
            });

            var outputReporter = new Progress<string>(p => progress?.Report(p));

            var result = await _youtubeDL.RunWithOptions(url, opts, cancellationToken, progress: progressReporter, output: outputReporter);

            if (!result.Success)
            {
                throw new InvalidOperationException($"下载字幕失败: {string.Join(", ", result.ErrorOutput)}");
            }

            progress?.Report($"字幕下载完成: {outputPath}");

            return outputPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "下载字幕失败: {Url}", url);
            throw;
        }
    }

    #endregion

    #region 公共方法 - 更新 yt-dlp

    public static async Task<string> UpdateYtDlpAsync(IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report("正在更新 yt-dlp...");

            var output = await _youtubeDL.RunUpdate();

            progress?.Report("更新完成");

            return output;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新 yt-dlp 失败");
            throw;
        }
    }

    #endregion
}


