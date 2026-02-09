using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using System;
using System.Diagnostics;
using System.Threading;
using VT.Module.BusinessObjects;



namespace VT.Module.Controllers;

public abstract class VideoProjectController : ObjectViewController<ObjectView, VideoProject>,IServices
{
    #region services
    public IServiceProvider ServiceProvider { get; set; }
    
    #endregion

    #region UI synchronization
    private SynchronizationContext? _uiSynchronizationContext;
    public SynchronizationContext? UiSynchronizationContext => _uiSynchronizationContext;
    protected IServices self => this;
    protected override void OnActivated()
    {
        base.OnActivated();
        _uiSynchronizationContext = SynchronizationContext.Current;
    }

    public void InvokeOnUIThread(Action action)
    {
        if (_uiSynchronizationContext != null)
        {
            _uiSynchronizationContext.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }
    #endregion

    #region constructor

    public VideoProjectController()
    {
    }
    #endregion

    #region helper methods
    protected VideoProject GetVideoProject()
    {
        if (View is DetailView detailView)
        {
            return detailView.CurrentObject as VideoProject ?? throw new InvalidOperationException("当前视图没有选中项目");
        }
        else
        {
            var project = ObjectSpace.CreateObject<VideoProject>();
            ObjectSpace.CommitChanges();
            project.Create();
            ObjectSpace.CommitChanges();
            return project;
        }
    }

    protected VideoProject? GetVideoProjectOrNull()
    {
        if (View is DetailView detailView)
        {
            return detailView.CurrentObject as VideoProject;
        }
        return null;
    }

    protected void ShowMessage(string message, InformationType infoType = InformationType.Info)
    {
        InvokeOnUIThread(() =>
        {
            Application.ShowViewStrategy.ShowMessage(message, infoType);
        });
    }

    protected void SetProjectProcessing(VideoProject project, string message)
    {
        InvokeOnUIThread(() =>
        {
            ObjectSpace.CommitChanges();
        });
    }

    protected void SetProjectError(VideoProject project, string errorMessage)
    {
        InvokeOnUIThread(() =>
        {
            ObjectSpace.CommitChanges();
        });
    }

    protected SimpleAction CreateAction(string id, string caption, SimpleActionExecuteEventHandler execute)
    {
        var action = new SimpleAction(this, id, null)
        {
            Caption = caption
        };
        action.Execute += execute;
        return action;
    }

    protected SimpleAction CreateAction(string id, string caption, string toolTip, SimpleActionExecuteEventHandler execute)
    {
        var action = new SimpleAction(this, id, null)
        {
            Caption = caption,
            ToolTip = toolTip
        };
        action.Execute += execute;
        return action;
    }

    protected AsyncSimpleAction CreateAsyncAction(string id, string caption, Func<CancellationToken, Task> asyncAction)
    {
        var action = new AsyncSimpleAction(this, id, null, asyncAction)
        {
            Caption = caption
        };
        return action;
    }

    protected AsyncSimpleAction CreateAsyncAction(string id, string caption, string toolTip, Func<CancellationToken, Task> asyncAction)
    {
        var action = new AsyncSimpleAction(this, id, null, asyncAction)
        {
            Caption = caption,
            ToolTip = toolTip
        };
        return action;
    }
    #endregion

    #region utils   



    public VideoProject GetCurrentVideoProject()
    {
        var videoProject = View.CurrentObject as VideoProject;
        if (videoProject == null)
        {
            throw new UserFriendlyException("未找到当前视频项目");
        }
        return videoProject;
    }

    public VideoProject? GetCurrentVideoProjectOrReturnNull()
    {
        return View.CurrentObject as VideoProject;
    }


    #endregion

}
