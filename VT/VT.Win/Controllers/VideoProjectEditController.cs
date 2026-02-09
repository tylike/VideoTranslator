using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using VT.Module.BusinessObjects;
using VT.Module.Controllers;
using VT.Win.Editors;

namespace VT.Win.Controllers;

public class VideoProjectEditController : VideoProjectController
{
    VideoPlayerPropertyEditor VideoPlayerPropertyEditor;
    ListView timeLineView;
    private TimeLineClip _currentClip;
    private bool _isSeekingFromTimeline;
    private SynchronizationContext _syncContext;
    [ActivatorUtilitiesConstructor]
    public VideoProjectEditController(IServiceProvider serviceProvider) : this()
      => ServiceProvider = serviceProvider;
    public VideoProjectEditController()
    {
        TargetViewType = ViewType.DetailView;
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

        VideoPlayerPropertyEditor = (VideoPlayerPropertyEditor)this.View.Items.Single(x => x.Id == nameof(VideoProject.Player));
        timeLineView = ((ListPropertyEditor)this.View.Items.Single(x => x.Id == nameof(VideoProject.Clips))).ListView;

        timeLineView.CurrentObjectChanged += TimeLineView_CurrentObjectChanged;
        VideoPlayerPropertyEditor.NotifyCurrentClipChanged += VideoPlayerPropertyEditor_CurrentClipChanged;
        
        self.progress.Report($"[VideoProjectEditController] OnViewControlsCreated 完成");
    }

    private void VideoPlayerPropertyEditor_CurrentClipChanged(object sender, TimeLineClip clip)
    {
        var progress = self.progress;
        progress.Report($"[VideoProjectEditController] VideoPlayerPropertyEditor_CurrentClipChanged，片段: {clip != null}，索引: {clip?.Index ?? -1}");
        
        if (_isSeekingFromTimeline)
        {
            progress.Report($"[VideoProjectEditController] 跳过 - 从时间线跳转");
            return;
        }

        if (clip != null && clip != _currentClip)
        {
            _currentClip = clip;
            progress.Report($"[VideoProjectEditController] 找到新片段 #{clip.Index}，更新timeLineView");
            
            _syncContext?.Post(_ => 
            {
                try
                {
                    //防止再次触发player的事件循环
                    timeLineView.CurrentObjectChanged -= TimeLineView_CurrentObjectChanged;
                    timeLineView.CurrentObject = clip;
                    progress.Report($"[VideoProjectEditController] 成功设置timeLineView.CurrentObject为片段 #{clip.Index}");
                }
                catch (Exception ex)
                {
                    progress.Report($"[VideoProjectEditController] 设置timeLineView.CurrentObject时出错: {ex.Message}");
                }
                finally
                {
                    timeLineView.CurrentObjectChanged += TimeLineView_CurrentObjectChanged;
                }
            }, null);
        }
        else if (clip == null && _currentClip != null)
        {
            progress.Report($"[VideoProjectEditController] 当前时间未找到片段");
            _currentClip = null;
        }
    }

    private void TimeLineView_CurrentObjectChanged(object sender, EventArgs e)
    {
        if (!ViewCurrentObject.AutoSyncCurrentClip)
            return;
        var progress = self.progress;
        var clip = timeLineView.CurrentObject as TimeLineClip;
        progress.Report($"[VideoProjectEditController] TimeLineView_CurrentObjectChanged，片段: {clip != null}，索引: {clip?.Index ?? -1}");
        
        if (clip != null && clip.SourceSRTClip != null)
        {
            progress.Report($"[VideoProjectEditController] 片段 #{clip.Index}，SourceSRTClip.Start: {clip.SourceSRTClip.Start.TotalSeconds:F2}s，End: {clip.SourceSRTClip.End.TotalSeconds:F2}s");            
            _currentClip = clip;
            _isSeekingFromTimeline = true;
            try
            {
                progress.Report($"[VideoProjectEditController] 调用SeekToClip为片段 #{clip.Index}");
                VideoPlayerPropertyEditor.JumpToClip(clip);
            }
            finally
            {
                _isSeekingFromTimeline = false;
                progress.Report($"[VideoProjectEditController] 重置isSeekingFromTimeline为false");
            }
        }
        else
        {
            progress.Report($"[VideoProjectEditController] 片段为null或SourceSRTClip为null");
        }
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        if (timeLineView != null)
        {
            timeLineView.CurrentObjectChanged -= TimeLineView_CurrentObjectChanged;
        }
        if (VideoPlayerPropertyEditor != null)
        {
            VideoPlayerPropertyEditor.NotifyCurrentClipChanged -= VideoPlayerPropertyEditor_CurrentClipChanged;
        }
    }
}
