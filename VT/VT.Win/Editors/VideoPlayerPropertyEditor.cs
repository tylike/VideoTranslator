using DevExpress.Diagram.Core;
using DevExpress.Diagram.Core.Layout;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Utils;
using DevExpress.Xpo;
using DevExpress.XtraDiagram;
using DevExpress.XtraDiagram.Commands;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using VT.Module.BusinessObjects;


namespace VT.Win.Editors;

/// <summary>
/// 视频播放器
/// </summary>
[PropertyEditor(typeof(VideoProject), "VideoPlayer", false)]
public class VideoPlayerPropertyEditor : PropertyEditor, IComplexControl, IComplexViewItem
{
    #region  公用
    #region 显示视图,备份不要删 
    ///// <summary>
    ///// 显示编辑视图
    ///// </summary>
    ///// <typeparam name="TDiagramItem">传入的可以是结点或线段对象的类型</typeparam>
    ///// <typeparam name="TPersistentObject">数据库端保存的对象的类型</typeparam>
    ///// <param name="shape">要显示的图形</param>
    ///// <param name="node">数据库对象</param>
    ///// <param name="isNew">是否是新建的</param>
    ///// <param name="arg">新建对象的参数，不是新建时为null</param>
    ///// <param name="updateContent">如何更新图形对象的内容</param>
    ///// <param name="addToFlow">如何将数据库对象填加到流程对象中去</param>
    ///// <param name="removeFromFlow">如何从流程对象中移除指定的对象</param>
    //void ShowView<TDiagramItem, TPersistentObject>(
    //    TDiagramItem shape,
    //    TPersistentObject node,
    //    bool isNew,
    //    DiagramAddingNewItemEventArgs arg,
    //    Action<TDiagramItem, TPersistentObject> updateContent,
    //    Action<TPersistentObject> addToFlow,
    //    Action<TPersistentObject> removeFromFlow
    //)
    //where TDiagramItem : DiagramItem
    //{
    //    if (node == null)
    //        throw new Exception("没有结点对象！");

    //    var view = Application.CreateDetailView(_os, node, false);
    //    var pv = new ShowViewParameters();
    //    pv.CreatedView = view;
    //    pv.TargetWindow = TargetWindow.NewModalWindow;

    //    object closeAction = null;

    //    #region dialogController

    //    var dc = new DialogController();
    //    dc.CancelAction.Active.RemoveItem("EditVisible");

    //    if (!isNew)
    //    {
    //        dc.CancelAction.Active["EditVisible"] = false;
    //    }

    //    dc.Accepting += (s, p1) =>
    //    {
    //        if (isNew)
    //        {
    //            addToFlow(node);
    //            //Flow.AddNode(node);
    //            shape.Tag = node;
    //        }
    //        updateContent(shape, node);
    //        //shape.Content = node.Caption;
    //        closeAction = s;
    //    };
    //    Action Cancel = () =>
    //    {
    //        if (isNew)
    //        {
    //            removeFromFlow(node);
    //            //Flow.RemoveNode(node);
    //            Flow.DeleteObject(node);
    //            this._diagram.DeleteItems(new[] { shape });
    //            arg.Cancel = true;
    //        }
    //    };
    //    dc.ViewClosing += (s, e) =>
    //    {
    //        if (closeAction == null)
    //        {
    //            Cancel();
    //            if (!isNew)
    //            {
    //                updateContent(shape, node);
    //                //shape.Content = node.Caption;
    //            }
    //        }
    //    };
    //    dc.Cancelling += (s, p1) =>
    //    {
    //        closeAction = s;
    //        Cancel();
    //    };

    //    dc.SaveOnAccept = false;
    //    pv.Controllers.Add(dc);
    //    #endregion
    //    Application.ShowViewStrategy.ShowView(pv, new ShowViewSource(null, null));
    //} 
    #endregion   
    #endregion

    #region ctor
    /// <summary>
    /// 构造流程设计器控件
    /// </summary>
    /// <param name="type"></param>
    /// <param name="viewItem"></param>
    public VideoPlayerPropertyEditor(Type type, IModelMemberViewItem viewItem) : base(type, viewItem)
    {

    }
    /// <summary>
    /// 销毁，清理事件,变量
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeFromPlayerEvents();
            player?.Dispose();
            player = null;
        }
        base.Dispose(disposing);
    }

    #endregion

    #region 创建控件
    /// <summary>
    /// 控件
    /// </summary>
    private VideoPlayer player;
    private SynchronizationContext _syncContext;

    /// <summary>
    /// 当前视频项目
    /// </summary>
    private VideoProject CurrentProject => PropertyValue as VideoProject;

    /// <summary>
    /// 当前片段改变事件
    /// </summary>
    public event EventHandler<TimeLineClip> NotifyCurrentClipChanged;

    /// <summary>
    /// 创建控件
    /// </summary>
    /// <returns></returns>
    protected override object CreateControlCore()
    {
        if (player == null)
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            player = new VideoPlayer();
            SubscribeToPlayerEvents();
            ReadValueCore();
        }
        return player;
    }

    private void SubscribeToPlayerEvents()
    {
        if (player != null)
        {
            player.CurrentClipChanged += NotifyPlayerCurrentClipChanged;
        }
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (player != null)
        {
            player.CurrentClipChanged -= NotifyPlayerCurrentClipChanged;
        }
    }

    private void NotifyPlayerCurrentClipChanged(object sender, TimeLineClip clip)
    {
        _syncContext?.Post(_ => NotifyCurrentClipChanged?.Invoke(this, clip), null);
    }

    /// <summary>
    /// 跳转到指定片段
    /// </summary>
    /// <param name="clip">时间线片段</param>
    public void JumpToClip(TimeLineClip clip)
    {
        if (player.VideoProject.AutoSyncCurrentClip)
        {
            Console.WriteLine($"[VideoPlayerPropertyEditor] 调用SeekToClip，片段: {clip != null}，索引: {clip?.Index ?? -1}");
            _syncContext?.Post(_ =>
            {
                Console.WriteLine($"[VideoPlayerPropertyEditor] SeekToClip - 调用player.SeekToClip，播放器: {player != null}");
                player?.SeekToClip(clip);
            }, null);
        }
    }

    #endregion


    #region 编辑器功能实现，数据库的交互        
    #region load from db
    /// <summary>
    /// 当前是否是正在加载数据中
    /// </summary>
    bool IsLoading;
    /// <summary>
    /// 从库里读值到界面
    /// </summary>
    protected override void ReadValueCore()
    {
        IsLoading = true;
        try
        {
            var project = CurrentProject;
            if (player != null)
            {
                player.VideoProject = project;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    bool _isRefreshing;
    /// <summary>
    /// 刷新控件，重画控件
    /// </summary>
    public override void Refresh()
    {
        _isRefreshing = true;
        ReadValueCore();
        base.Refresh();
        _isRefreshing = false;
    }
    #endregion

    #region get control value
    /// <summary>
    /// 取得控件值，已经没有作用，保存值的动作由流程对象进行管理
    /// </summary>
    /// <returns></returns>
    protected override object GetControlValueCore()
    {
        return null;
    }
    #endregion

    #region setup
    private IObjectSpace _os;
    private XafApplication Application;
    /// <summary>
    /// 控件内部初始化时调用
    /// </summary>
    /// <param name="objectSpace"></param>
    /// <param name="application"></param>
    public void Setup(IObjectSpace objectSpace, XafApplication application)
    {
        this._os = objectSpace;
        this.Application = application;
    }
    #endregion

    #endregion

}

#pragma warning disable WFO1000 // 缺少属性内容的代码序列化配置

public class VideoPlayer : SimpleVideoPlayer.VideoPlayer
{
    public VideoProject VideoProject
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                LoadVideoAndSubtitles(value);
            }
        }
    }

    private void LoadVideoAndSubtitles(VideoProject project)
    {
        if (project == null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(project.OutputVideoPath) && System.IO.File.Exists(project.OutputVideoPath))
        {
            base.LoadVideo(project.OutputVideoPath);
        }
        else if (!string.IsNullOrEmpty(project.SourceVideoPath) && System.IO.File.Exists(project.SourceVideoPath))
        {
            base.LoadVideo(project.SourceVideoPath);
        }

        if (!string.IsNullOrEmpty(project.SourceSubtitlePath) && System.IO.File.Exists(project.SourceSubtitlePath))
        {
            base.LoadSubtitle(project.SourceSubtitlePath);
        }

        if (!string.IsNullOrEmpty(project.TranslatedSubtitlePath) && System.IO.File.Exists(project.TranslatedSubtitlePath))
        {
            base.LoadSubtitle(project.TranslatedSubtitlePath);
        }
    }


    public Action<object, TimeLineClip> CurrentClipChanged { get; set; }

    public void SeekToClip(TimeLineClip clip)
    {
        this.SeekTo(clip.SourceSRTClip.Start.TotalSeconds);
    }
    TimeLineClip _currentClip;
    protected override void OnTimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
    {
        base.OnTimeChanged(sender, e);
        var time = ((double)(e.Time)) / 1000;
        if (VideoProject != null)
        {
            var clip = VideoProject.Clips.FirstOrDefault(c => c.SourceSRTClip.Start.TotalSeconds <= time && c.SourceSRTClip.End.TotalSeconds >= time);
            if (clip != _currentClip)
            {
                _currentClip = clip;
                CurrentClipChanged?.Invoke(this, clip);
            }

        }
    }
}
#pragma warning restore WFO1000 // 缺少属性内容的代码序列化配置
