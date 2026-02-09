# TimeLine 控件库

这是一个基于WPF的时间线编辑控件，用于显示和编辑视频项目的各种时间轴数据。

## 功能特性

- 支持显示9种不同的时间轴行：时间轴、VAD、源SRT、目标翻译SRT、源音频片段、目标翻译音频片段、调整音频片段、源人声音频、源背景音频
- 支持缩放功能（0.5x - 3.0x）
- 支持点击播放音频片段
- 支持水平和垂直滚动
- 模块化设计，易于扩展

## 使用方法

### 在WinForms中使用

```csharp
using TimeLine.Extensions;
using VT.Module.BusinessObjects;

// 方法1：直接传入VideoProject
var host = this.CreateTimeLineHost(videoProject);

// 方法2：传入ViewModel
var viewModel = new TimeLineViewModel { VideoProject = videoProject };
var host = this.CreateTimeLineHost(viewModel);

// 方法3：自定义音频播放服务
var audioPlayerService = new AudioPlayerService();
var host = this.CreateTimeLineHost(videoProject, audioPlayerService);
```

### 在WPF中使用

```csharp
using TimeLine.Views;
using TimeLine.Controls;
using VT.Module.BusinessObjects;

// 方法1：使用TimeLineWindow
var window = new TimeLineWindow(videoProject);
window.Show();

// 方法2：直接使用SimpleTimeLinePanel
var panel = new SimpleTimeLinePanel
{
    ViewModel = new TimeLineViewModel { VideoProject = videoProject },
    AudioPlayerService = new AudioPlayerService()
};
```

## 数据结构

控件使用 `VT.Module.BusinessObjects` 中的数据结构：

- `VideoProject`: 视频项目
- `TimeLineClip`: 时间轴片段
- `SRTClip`: 字幕片段
- `AudioClip`: 音频片段
- `VadSegment`: VAD片段

## 控件结构

```
SimpleTimeLinePanel (主控件)
├── LeftPanel (左侧标题区域)
│   └── TimeLineRow (行标题)
└── RightPanel (右侧时间轴区域)
    └── TimeLineRowControl (行内容)
        └── WaveformControl (波形/片段控件)
```

## 自定义样式

可以通过修改 `Themes/ControlStyles.xaml` 来自定义控件样式。

## 依赖项

- .NET 9.0
- WPF
- VT.Module (BusinessObjects)
- WindowsFormsIntegration (用于WinForms集成)
