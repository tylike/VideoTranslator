using System;
using System.IO;
using System.Threading.Tasks;

namespace SimpleVideoPlayer
{
    public class VideoDownloaderExample
    {
        public static async Task Example1_DownloadYouTubeVideo()
        {
            using var downloader = new VideoDownloader();

            downloader.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"下载进度: {e.Percentage}%");
            };

            downloader.DownloadCompleted += (sender, e) =>
            {
                Console.WriteLine($"下载完成! 保存到: {e.OutputPath}");
            };

            downloader.DownloadError += (sender, e) =>
            {
                Console.WriteLine($"下载失败: {e.ErrorMessage}");
            };

            string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "downloaded_video.mp4");

            Console.WriteLine("开始下载...");
            bool success = await downloader.DownloadVideoAsync(youtubeUrl, outputPath);

            Console.WriteLine($"下载结果: {(success ? "成功" : "失败")}");
        }

        public static async Task Example2_DownloadWithCancellation()
        {
            using var downloader = new VideoDownloader();

            downloader.ProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"下载进度: {e.Percentage}%");
            };

            string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "downloaded_video.mp4");

            var downloadTask = downloader.DownloadVideoAsync(youtubeUrl, outputPath);

            await Task.Delay(3000);

            Console.WriteLine("取消下载...");
            downloader.CancelDownload();

            await downloadTask;
        }

        public static async Task Example3_DownloadDifferentFormats()
        {
            using var downloader = new VideoDownloader();

            string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            Console.WriteLine("下载为 MP4 格式...");
            await downloader.DownloadVideoAsync(
                youtubeUrl,
                Path.Combine(desktopPath, "video.mp4")
            );

            Console.WriteLine("下载为 MKV 格式...");
            await downloader.DownloadVideoAsync(
                youtubeUrl,
                Path.Combine(desktopPath, "video.mkv")
            );

            Console.WriteLine("下载为 AVI 格式...");
            await downloader.DownloadVideoAsync(
                youtubeUrl,
                Path.Combine(desktopPath, "video.avi")
            );
        }

        public static async Task Example4_DownloadWithDifferentResolutions()
        {
            string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            Console.WriteLine("下载 1080p 视频...");
            using var downloader1080p = new VideoDownloader(new[]
            {
                "--no-xlib",
                "--no-video-title-show",
                "--no-snapshot-preview",
                "--no-audio-time-stretch",
                "--preferred-resolution=1080"
            });
            await downloader1080p.DownloadVideoAsync(
                youtubeUrl,
                Path.Combine(desktopPath, "video_1080p.mp4")
            );

            Console.WriteLine("下载 720p 视频...");
            using var downloader720p = new VideoDownloader(new[]
            {
                "--no-xlib",
                "--no-video-title-show",
                "--no-snapshot-preview",
                "--no-audio-time-stretch",
                "--preferred-resolution=720"
            });
            await downloader720p.DownloadVideoAsync(
                youtubeUrl,
                Path.Combine(desktopPath, "video_720p.mp4")
            );

            Console.WriteLine("下载 480p 视频...");
            using var downloader480p = new VideoDownloader(new[]
            {
                "--no-xlib",
                "--no-video-title-show",
                "--no-snapshot-preview",
                "--no-audio-time-stretch",
                "--preferred-resolution=576"
            });
            await downloader480p.DownloadVideoAsync(
                youtubeUrl,
                Path.Combine(desktopPath, "video_480p.mp4")
            );
        }

        public static async Task Example5_DownloadFromOtherSources()
        {
            using var downloader = new VideoDownloader();

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            Console.WriteLine("下载 HLS 流...");
            await downloader.DownloadVideoAsync(
                "https://example.com/stream.m3u8",
                Path.Combine(desktopPath, "hls_stream.mp4")
            );

            Console.WriteLine("下载 RTMP 流...");
            await downloader.DownloadVideoAsync(
                "rtmp://example.com/live/stream",
                Path.Combine(desktopPath, "rtmp_stream.mp4")
            );

            Console.WriteLine("下载 HTTP 视频文件...");
            await downloader.DownloadVideoAsync(
                "http://example.com/video.mp4",
                Path.Combine(desktopPath, "http_video.mp4")
            );
        }
    }
}
