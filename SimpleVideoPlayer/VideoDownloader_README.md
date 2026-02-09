# VLC 视频下载功能使用说明

## 概述

VLC 可以通过 Stream Output (sout) 功能将正在播放的视频流保存到本地文件，实现视频下载功能。本实现支持：

- YouTube 视频下载
- HLS 流下载
- RTMP 流下载
- HTTP 视频文件下载
- 多种输出格式（MP4、MKV、AVI）
- 下载进度监控
- 下载取消功能

## 核心文件

1. **VideoDownloader.cs** - 视频下载器核心类
2. **VideoDownloadForm.cs** - 下载界面窗体
3. **VideoDownloaderExample.cs** - 使用示例

## 快速开始

### 1. 基本下载

```csharp
using var downloader = new VideoDownloader();

string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
string outputPath = @"C:\Videos\downloaded_video.mp4";

bool success = await downloader.DownloadVideoAsync(youtubeUrl, outputPath);
```

### 2. 监控下载进度

```csharp
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

await downloader.DownloadVideoAsync(url, outputPath);
```

### 3. 取消下载

```csharp
using var downloader = new VideoDownloader();

var downloadTask = downloader.DownloadVideoAsync(url, outputPath);

// 3秒后取消
await Task.Delay(3000);
downloader.CancelDownload();

await downloadTask;
```

## 高级功能

### 1. 控制下载分辨率

在 VideoDownloader.cs 的 InitializeVLC 方法中修改 `--preferred-resolution` 参数：

```csharp
private void InitializeVLC()
{
    _libVLC = new LibVLC(
        "--no-xlib",
        "--no-video-title-show",
        "--preferred-resolution=1080"  // 设置首选分辨率
    );
}
```

可选值：
- `-1` - 最佳可用（默认）
- `1080` - 1080p 全高清
- `720` - 720p 高清
- `576` - 576p 标清
- `360` - 360p 低清
- `240` - 240p 极低清

### 2. 支持的输出格式

| 格式 | 扩展名 | 说明 |
|------|--------|------|
| MP4 | .mp4 | 最常用，兼容性最好 |
| MKV | .mkv | 支持多音轨和字幕 |
| AVI | .avi | 传统格式 |

### 3. 支持的视频源

| 视频源 | 示例 URL | 说明 |
|--------|----------|------|
| YouTube | `https://www.youtube.com/watch?v=xxx` | 最常用的视频源 |
| HLS | `https://example.com/stream.m3u8` | 直播流 |
| RTMP | `rtmp://example.com/live/stream` | 实时流媒体 |
| HTTP | `http://example.com/video.mp4` | 直接下载视频文件 |

## 工作原理

### VLC Stream Output (sout) 机制

VLC 使用 `:sout` 参数来实现流输出功能：

```csharp
var mediaOptions = new string[]
{
    ":sout=#transcode{vcodec=h264,acodec=mpga}:standard{access=file,mux=mp4,dst=\"output.mp4\"}",
    ":sout-keep"
};
```

### sout 参数说明

- `:sout` - 流输出配置
- `#transcode{vcodec=h264,acodec=mpga}` - 转码配置
  - `vcodec=h264` - 视频编码器
  - `acodec=mpga` - 音频编码器
- `:standard{access=file,mux=mp4,dst="output.mp4"}` - 输出配置
  - `access=file` - 文件访问方式
  - `mux=mp4` - 容器格式
  - `dst="output.mp4"` - 输出文件路径
- `:sout-keep` - 保持流输出连接

### 下载流程

```
1. 创建 Media 对象，指定视频 URL
2. 添加 :sout 参数配置输出
3. 创建 MediaPlayer 并关联 Media
4. 调用 Play() 开始播放（实际上是下载）
5. 监听 PositionChanged 事件获取进度
6. 监听 EndReached 事件确认完成
7. 下载完成后停止播放
```

## 使用示例

### 示例 1：下载 YouTube 视频

```csharp
using var downloader = new VideoDownloader();

downloader.ProgressChanged += (sender, e) =>
{
    Console.WriteLine($"下载进度: {e.Percentage}%");
};

string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
string outputPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    "youtube_video.mp4"
);

bool success = await downloader.DownloadVideoAsync(youtubeUrl, outputPath);
Console.WriteLine($"下载结果: {(success ? "成功" : "失败")}");
```

### 示例 2：下载不同分辨率

```csharp
string youtubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

// 下载 1080p
using var downloader1080p = new VideoDownloader();
downloader1080p.LibVLC = new LibVLCSharp.Shared.LibVLC("--preferred-resolution=1080");
await downloader1080p.DownloadVideoAsync(
    youtubeUrl,
    Path.Combine(desktopPath, "video_1080p.mp4")
);

// 下载 720p
using var downloader720p = new VideoDownloader();
downloader720p.LibVLC = new LibVLCSharp.Shared.LibVLC("--preferred-resolution=720");
await downloader720p.DownloadVideoAsync(
    youtubeUrl,
    Path.Combine(desktopPath, "video_720p.mp4")
);
```

### 示例 3：使用下载界面

```csharp
var downloadForm = new VideoDownloadForm();
downloadForm.Show();

// 或者
var downloadForm = new VideoDownloadForm();
await downloadForm.DownloadYouTubeVideoAsync(
    "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
    @"C:\Videos\downloaded_video.mp4"
);
```

## 注意事项

### 1. 性能考虑

- **CPU 占用**：下载过程中会进行转码，CPU 占用较高
- **磁盘空间**：确保有足够的磁盘空间存储下载的视频
- **网络带宽**：高分辨率视频需要更快的网络连接

### 2. 限制和问题

- **YouTube 变更**：YouTube 可能会更新其 API 或流媒体格式，导致下载失败
- **版权保护**：某些视频可能有版权保护，无法下载
- **网络不稳定**：网络中断可能导致下载失败

### 3. 最佳实践

- 使用适当的分辨率（720p 通常是性能和质量的最佳平衡）
- 监控下载进度，及时处理错误
- 提供取消功能，让用户可以中断下载
- 使用 try-catch 处理异常

### 4. 错误处理

```csharp
try
{
    bool success = await downloader.DownloadVideoAsync(url, outputPath);
    if (!success)
    {
        Console.WriteLine("下载失败");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("下载被取消");
}
catch (Exception ex)
{
    Console.WriteLine($"下载异常: {ex.Message}");
}
```

## 事件说明

### ProgressChanged 事件

在下载过程中持续触发，报告下载进度：

```csharp
downloader.ProgressChanged += (sender, e) =>
{
    Console.WriteLine($"进度: {e.Progress} ({e.Percentage}%)");
};
```

### DownloadCompleted 事件

下载完成时触发：

```csharp
downloader.DownloadCompleted += (sender, e) =>
{
    Console.WriteLine($"下载完成: {e.OutputPath}");
    Console.WriteLine($"成功: {e.Success}");
};
```

### DownloadError 事件

下载过程中发生错误时触发：

```csharp
downloader.DownloadError += (sender, e) =>
{
    Console.WriteLine($"错误: {e.ErrorMessage}");
};
```

## 集成到现有项目

### 在 VideoPlayer 中添加下载功能

```csharp
public class VideoPlayer : Control
{
    private VideoDownloader _downloader;

    public VideoPlayer()
    {
        InitializeVLC();
        InitializeControls();
        _downloader = new VideoDownloader();
    }

    public async Task<bool> DownloadCurrentVideoAsync(string outputPath)
    {
        if (string.IsNullOrEmpty(CurrentVideoPath))
        {
            return false;
        }

        return await _downloader.DownloadVideoAsync(CurrentVideoPath, outputPath);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _downloader?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## 总结

VLC 的 Stream Output 功能为视频下载提供了强大而灵活的解决方案。通过本实现，你可以：

1. ✅ 下载 YouTube 视频
2. ✅ 下载 HLS 和 RTMP 流
3. ✅ 控制下载分辨率
4. ✅ 监控下载进度
5. ✅ 取消下载操作
6. ✅ 支持多种输出格式

这个功能可以很好地集成到你的 VideoTranslator 项目中，为用户提供视频下载能力！
