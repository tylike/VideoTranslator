# VideoEditor 项目文档

## 项目概述

VideoEditor 是一个基于 WPF 的视频翻译编辑器，用于处理视频翻译项目的音频处理、字幕识别、时间线编辑等功能。该项目集成了音频分离、语音识别、字幕翻译等核心功能。

## 技术栈

- **框架**: .NET 9.0 (Windows)
- **UI 框架**: WPF
- **MVVM 框架**: CommunityToolkit.Mvvm
- **ORM**: DevExpress XPO
- **日志**: Serilog
- **视频播放**: LibVLCSharp
- **依赖注入**: Microsoft.Extensions.DependencyInjection

## 项目结构

```
VideoEditor/
├── Services/
│   └── SimpleProgressService.cs      # 进度服务实现
├── ViewModels/
│   └── VideoEditorViewModel.cs       # 主视图模型
├── Windows/
│   ├── NewProjectWindow.xaml        # 新建项目窗口
│   ├── ProjectSelectionWindow.xaml  # 项目选择窗口
│   └── ...
├── App.xaml                         # 应用程序入口
├── MainWindow.xaml                  # 主窗口
└── LoggerService.cs                 # 日志服务配置
```

## 核心业务流程

### 1. 项目初始化流程

```
启动应用
    ↓
初始化服务容器 (ServiceProvider)
    ↓
显示项目选择窗口
    ↓
加载选中的项目
    ↓
初始化时间线视图
```

### 2. 音频处理流程

音频处理是 VideoEditor 的核心功能，包含以下步骤：

```
1. 提取音频 (ExtractAudio)
   ├─ 从源视频提取音频
   ├─ 创建静音视频
   └─ 创建源音频轨道 (MediaType.源音频)

2. 分离音频 (SeparateAudio)
   ├─ 将源音频分离为人声和背景音
   ├─ 创建说话音频轨道 (MediaType.说话音频)
   └─ 创建背景音频轨道 (MediaType.背景音频)

3. 语音识别 (SpeechRecognitionWithVad)
   ├─ 使用 VAD (Voice Activity Detection) 增强识别
   ├─ 生成源字幕VAD轨道 (MediaType.源字幕Vad)
   └─ 生成带时间戳的字幕片段

4. 分段音频 (SegmentSourceAudioBySrt)
   ├─ 根据 VAD 字幕片段分段说话音频
   ├─ 根据 VAD 字幕片段分段源音频
   └─ 生成对应的音频片段
```

### 3. 时间线数据结构

时间线使用 `Tracks` (轨道) 和 `Segments` (片段) 的数据结构：

```
VideoProject
    └── Tracks (轨道集合)
        ├── TrackInfo (基类)
        │   ├── AudioTrackInfo (音频轨道)
        │   │   └── Segments → AudioClip[]
        │   └── SRTTrackInfo (字幕轨道)
        │       └── Segments → SRTClip[]
        └── Media (媒体源)
            ├── AudioSource
            └── SRTSource
```

### 4. 媒体类型 (MediaType)

项目支持多种媒体类型：

| 类型 | 说明 |
|------|------|
| 源视频 | 原始视频文件 |
| 源音频 | 从视频提取的原始音频 |
| 说话音频 | 分离后的人声音频 |
| 背景音频 | 分离后的背景音乐 |
| 静音视频 | 去除音频的视频 |
| 源字幕 | 原始字幕 |
| 源字幕Vad | VAD 识别生成的字幕 |
| 目标字幕 | 翻译后的字幕 |
| 目标音频 | 翻译后的音频 |
| 调整音频 | 调整后的音频 |

## 核心组件说明

### MainWindow (主窗口)

主窗口负责：
- 项目加载和管理
- 音频处理流程控制
- 视频和字幕播放
- 时间线集成
- 状态栏进度显示

### SimpleProgressService (进度服务)

进度服务负责：
- 显示进度条
- 更新状态消息
- 报告处理进度
- 重置进度状态

### TimeLineViewModel (时间线视图模型)

时间线视图模型负责：
- 加载和显示各种轨道数据
- 管理 VAD、字幕、音频等片段
- 更新时间标尺
- 重新加载数据

### VideoProject (视频项目)

视频项目是核心业务对象，包含：
- 媒体源集合 (MediaSources)
- 轨道集合 (Tracks)
- 各种辅助方法 (GetXxx, GetXxxTrack)

## 关键方法说明

### VideoProject 核心方法

```csharp
// 获取媒体源
VideoSource? GetSourceVideo()
AudioSource? GetAudioSource()
AudioSource? GetVocalsdAudio()
AudioSource? GetBackgrounddAudio()

// 获取轨道
AudioTrackInfo? GetSourceAudioTrack()
AudioTrackInfo? GetVocalsAudioTrack()
SRTTrackInfo GetVadSubtitleTrackInfo()
```

### AudioTrackInfo 核心方法

```csharp
// 根据 SRT 字幕分段音频
Task SegmentSourceAudioBySrt(SRTTrackInfo srt)
```

## 数据流转

### 音频处理数据流

```
源视频文件
    ↓ ExtractAudio
源音频轨道 (源音频)
    ↓ SeparateAudio
说话音频轨道 + 背景音频轨道
    ↓ SpeechRecognitionWithVad
源字幕VAD轨道 (带时间戳)
    ↓ SegmentSourceAudioBySrt
分段后的音频片段
```

### 时间线显示数据流

```
VideoProject.Tracks
    ↓ TimeLineViewModel.LoadData()
TimeLineModel (时间线数据模型)
    ↓ TimeLinePanel
时间线 UI 显示
```

## 服务注册

应用启动时注册以下服务：

```csharp
services.AddVideoTranslatorServices(null!);
services.AddSingleton<IProgressService, Services.SimpleProgressService>();
```

重要：`IProgressService` 需要在 VideoEditor 项目中覆盖注册，以确保使用正确的实现。

## 日志配置

日志使用 Serilog，配置如下：

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/videoeditor-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

## 构建和运行

### 构建项目

```bash
cd VideoEditor
dotnet build
```

### 运行项目

```bash
dotnet run
```

### 依赖项目

VideoEditor 依赖以下项目：
- **TimeLine**: 时间线控件和视图模型
- **VideoPlayer**: 视频播放器组件
- **Logic**: 业务逻辑层
- **VT.Module**: DevExpress 业务对象模块

## 常见问题

### 状态栏不更新

确保：
1. `SimpleProgressService` 正确注册为单例
2. `ServiceProvider` 正确传递给 DevExpress Session
3. 进度服务正确调用 `ShowProgress()` 和 `SetStatusMessage()`

### 时间线不更新

音频处理后需要调用：
```csharp
_viewModel.TimeLineViewModel?.ReloadData();
```

### 源音频片段为空

确保：
1. 调用 `SegmentSourceAudioBySrt` 对源音频轨道进行分段
2. VAD 字幕轨道存在且包含片段
3. 音频文件路径正确

## 开发规范

### 代码风格

- 使用面向对象的编程方法
- 类型和代码拆分为职责较小的类（通常 300 行以内）
- 使用扩展方法扩展功能
- 使用 `#region/#endregion` 包裹同类型代码
- 添加必要的注释说明业务逻辑

### MVVM 模式

- 使用 `ObservableProperty` 属性
- 使用 `RelayCommand` 命令
- 视图模型与视图分离

## 许可证

本项目仅供学习和研究使用。
