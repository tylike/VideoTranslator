using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using VT.Module.BusinessObjects;
using System.IO;
using NAudio.Wave;

namespace VideoEditor.ViewModels;

public partial class VideoEditorViewModel : ObservableObject
{
    #region 字段

    private static readonly ILogger _logger = LoggerService.ForContext<VideoEditorViewModel>();

    #endregion

    #region 构造函数

    public VideoEditorViewModel()
    {
        _logger.Information("VideoEditorViewModel 创建");
    }

    #endregion
}
