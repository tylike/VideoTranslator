# TimeLine 控件使用示例

## 在 WinForms 中使用

### 方法 1：使用扩展方法（推荐）

```csharp
using System.Windows.Forms;
using TimeLine.Extensions;
using VT.Module.BusinessObjects;

public class MyForm : Form
{
    private VideoProject _videoProject;

    public MyForm(VideoProject videoProject)
    {
        _videoProject = videoProject;
        InitializeTimeLine();
    }

    private void InitializeTimeLine()
    {
        // 使用扩展方法创建时间线控件
        this.CreateTimeLineHost(_videoProject);
    }
}
```

### 方法 2：使用 ViewModel

```csharp
using System.Windows.Forms;
using TimeLine.Extensions;
using TimeLine.ViewModels;
using TimeLine.Services;

public class MyForm : Form
{
    private VideoProject _videoProject;

    public MyForm(VideoProject videoProject)
    {
        _videoProject = videoProject;
        InitializeTimeLine();
    }

    private void InitializeTimeLine()
    {
        // 创建 ViewModel
        var viewModel = new TimeLineViewModel
        {
            VideoProject = _videoProject
        };

        // 创建自定义音频播放服务
        var audioPlayerService = new AudioPlayerService();

        // 使用扩展方法创建时间线控件
        this.CreateTimeLineHost(viewModel);
    }
}
```

### 方法 3：完全自定义

```csharp
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using TimeLine.Controls;
using TimeLine.ViewModels;
using TimeLine.Services;

public class MyForm : Form
{
    private VideoProject _videoProject;

    public MyForm(VideoProject videoProject)
    {
        _videoProject = videoProject;
        InitializeTimeLine();
    }

    private void InitializeTimeLine()
    {
        // 创建 ElementHost
        var host = new ElementHost
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.Transparent
        };

        // 创建 SimpleTimeLinePanel
        var timeLinePanel = new SimpleTimeLinePanel
        {
            ViewModel = new TimeLineViewModel { VideoProject = _videoProject },
            AudioPlayerService = new AudioPlayerService()
        };

        // 将 WPF 控件添加到 WinForms
        host.Child = timeLinePanel;
        this.Controls.Add(host);
    }
}
```

## 在 WPF 中使用

### 方法 1：使用 TimeLineWindow

```csharp
using TimeLine.Views;
using VT.Module.BusinessObjects;

public class MyWpfWindow : Window
{
    private VideoProject _videoProject;

    public MyWpfWindow(VideoProject videoProject)
    {
        _videoProject = videoProject;
        ShowTimeLine();
    }

    private void ShowTimeLine()
    {
        var timeLineWindow = new TimeLineWindow(_videoProject);
        timeLineWindow.Show();
    }
}
```

### 方法 2：直接使用 SimpleTimeLinePanel

```csharp
using System.Windows;
using TimeLine.Controls;
using TimeLine.ViewModels;
using TimeLine.Services;
using VT.Module.BusinessObjects;

public class MyWpfWindow : Window
{
    private VideoProject _videoProject;

    public MyWpfWindow(VideoProject videoProject)
    {
        _videoProject = videoProject;
        InitializeComponent();
        InitializeTimeLine();
    }

    private void InitializeTimeLine()
    {
        var timeLinePanel = new SimpleTimeLinePanel
        {
            ViewModel = new TimeLineViewModel { VideoProject = _videoProject },
            AudioPlayerService = new AudioPlayerService()
        };

        this.Content = timeLinePanel;
    }
}
```

## 自定义音频播放服务

如果需要自定义音频播放逻辑，可以实现 `IAudioPlayerService` 接口：

```csharp
using TimeLine.Services;

public class CustomAudioPlayerService : IAudioPlayerService
{
    public bool IsPlaying { get; private set; }
    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackEnded;

    public async Task PlayAsync(string filePath)
    {
        // 自定义播放逻辑
        // 例如：使用其他音频库或播放器
        IsPlaying = true;
        PlaybackStarted?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    public void Stop()
    {
        // 自定义停止逻辑
        IsPlaying = false;
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
}
```

然后使用自定义服务：

```csharp
var customPlayer = new CustomAudioPlayerService();
this.CreateTimeLineHost(_videoProject, customPlayer);
```

## 响应事件

SimpleTimeLinePanel 支持多种事件，可以响应用户交互：

```csharp
var timeLinePanel = new SimpleTimeLinePanel
{
    ViewModel = new TimeLineViewModel { VideoProject = _videoProject },
    AudioPlayerService = new AudioPlayerService()
};

// 订阅缩放变化事件
timeLinePanel.Data.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(TimeLineData.ZoomFactor))
    {
        var zoomFactor = timeLinePanel.Data.ZoomFactor;
        Console.WriteLine($"缩放因子: {zoomFactor:F2}");
    }
};
```

## 注意事项

1. **线程安全**：确保在 UI 线程上操作控件
2. **资源释放**：窗口关闭时，音频播放服务会自动停止
3. **数据绑定**：VideoProject 的变化会自动反映在时间线上
4. **性能优化**：对于大量数据，考虑虚拟化或分页加载

## 完整示例

参见 [TimeLineExample.cs](TimeLineExample.cs) 获取完整的使用示例。
