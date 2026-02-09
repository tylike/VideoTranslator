#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using VideoTranslator.Interfaces;

namespace VT.Module.Controllers;

public delegate Task AsyncSimpleActionExecuteEventHandler(object sender, SimpleActionExecuteEventArgs e);

public class AsyncSimpleAction : SimpleAction
{
    private Task? _pendingTask;
    private readonly object _lock = new object();
    private bool autoControlButtonState = true;
    private SynchronizationContext? _uiSynchronizationContext;

    public AsyncSimpleAction(Controller controller, string id, string? category, bool? autoControlButtonState = true)
        : base(controller, id, category)
    {
        if (autoControlButtonState.HasValue)
            this.autoControlButtonState = autoControlButtonState.Value;
        _uiSynchronizationContext = SynchronizationContext.Current;
    }

    public AsyncSimpleAction(Controller controller, string id, string? category, Func<CancellationToken, Task> asyncAction)
        : base(controller, id, category)
    {
        _uiSynchronizationContext = SynchronizationContext.Current;
        AsyncExecute += async (sender, e) => await asyncAction(GetCancellationToken());
    }

    public event AsyncSimpleActionExecuteEventHandler? AsyncExecute;

    public bool AutoControlButtonState
    {
        get => autoControlButtonState;
        set => autoControlButtonState = value;
    }
    IProgressService? progress => (this.Controller as IServices)?.progress;
    protected override void OnExecuting(CancelEventArgs e)
    {
        base.OnExecuting(e);
        if (!AutoControlButtonState)
        {
            return;
        }

        lock (_lock)
        {
            if (_pendingTask != null && !_pendingTask.IsCompleted)
            {
                return;
            }

            if (AsyncExecute != null)
            {
                var task = AsyncExecute(this, new SimpleActionExecuteEventArgs(this, base.SelectionContext));
                if (task != null)
                {
                    _pendingTask = task;

                    task.ContinueWith(
                        x =>
                        {
                            InvokeOnUIThread(() =>
                                {
                                    Enabled["Processing"] = true;

                                    if (x.IsFaulted)
                                    {
                                        this.OnHandleException(x.Exception);
                                        var exception = x.Exception?.InnerException ?? x.Exception;
                                        progress?.Error(exception?.Message ?? "任务执行失败");
                                        progress?.SetStatusMessage($"执行失败: {this.Caption}");
                                    }
                                    else if (x.IsCanceled)
                                    {
                                        progress?.SetStatusMessage($"任务已取消: {this.Caption}");
                                    }
                                    else
                                    {
                                        progress?.SetStatusMessage($"完成执行{this.Caption}");
                                    }
                                });
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default
                    );

                    Enabled["Processing"] = false;
                    progress?.ShowProgress();
                    progress?.SetStatusMessage($"准备执行{this.Caption}");
                }
            }
        }
    }

    protected override void OnHandleException(Exception e)
    {
        progress?.Error(e.Message);
        base.OnHandleException(e);

    }

    private void InvokeOnUIThread(Action action)
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
    public CancellationToken GetCancellationToken()
    {
        return new CancellationTokenSource().Token;
    }
}
