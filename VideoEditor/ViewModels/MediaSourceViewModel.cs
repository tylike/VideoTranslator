using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using System.Collections.ObjectModel;
using VT.Module.BusinessObjects;

namespace VideoEditor.ViewModels;

public partial class MediaSourceViewModel : ObservableObject
{
    #region 字段

    private static readonly ILogger _logger = Log.ForContext<MediaSourceViewModel>();
    private readonly VideoProject _project;

    #endregion

    #region 属性

    [ObservableProperty]
    private ObservableCollection<MediaSource> _mediaSources = new();

    [ObservableProperty]
    private MediaSource? _selectedMediaSource;

    #endregion

    #region 构造函数

    public MediaSourceViewModel(VideoProject project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _logger.Information("MediaSourceViewModel 创建");
    }

    #endregion

    #region 公共方法

    public void LoadMediaSources()
    {
        try
        {
            MediaSources.Clear();
            
            var sources = _project.MediaSources
                .OrderBy(m => m.MediaType)
                .ThenBy(m => m.Name)
                .ToList();

            foreach (var source in sources)
            {
                MediaSources.Add(source);
            }

            _logger.Information("已加载 {Count} 个媒体源", MediaSources.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载媒体源列表时发生错误");
        }
    }

    public void AddMediaSource(MediaSource mediaSource)
    {
        try
        {
            MediaSources.Add(mediaSource);
            _logger.Information("媒体源已添加: {Name}", mediaSource.Name);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "添加媒体源时发生错误");
        }
    }

    public void RemoveMediaSource(MediaSource mediaSource)
    {
        try
        {
            MediaSources.Remove(mediaSource);
            _logger.Information("媒体源已移除: {Name}", mediaSource.Name);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "移除媒体源时发生错误");
        }
    }

    #endregion
}
