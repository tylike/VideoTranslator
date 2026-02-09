using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Win.Controls;
using DevExpress.ExpressApp.Win.Templates.Ribbon;
using DevExpress.ExpressApp.Win.Utils;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors.Repository;
using VT.Win.Forms;

namespace VT.Win.Services;
using MessageType = VideoTranslator.Interfaces.MessageType;

public class RibbonTemplateCustomizer
{
    #region Private Fields

    private BarStaticItem _statusItem;
    private BarEditItem _progressItem;
    private BarButtonItem _logViewerButton;
    private RepositoryItemProgressBar _progressRepository;
    private System.Windows.Forms.Form _mainForm;
    private LogViewerForm _logViewerForm;

    #endregion

    #region Public Properties

    public BarStaticItem StatusItem => _statusItem;
    public BarEditItem ProgressItem => _progressItem;

    #endregion

    #region Public Methods

    public void CustomizeTemplate(IFrameTemplate frameTemplate)
    {
        var lightStyleMainRibbonForm = frameTemplate as LightStyleMainRibbonForm;
        if (lightStyleMainRibbonForm == null)
        {
            return;
        }

        var template = lightStyleMainRibbonForm;
        var barManagerHolder = template as IBarManagerHolder;
        if (barManagerHolder?.BarManager == null)
        {
            return;
        }

        var barManager = barManagerHolder.BarManager;

        #region Initialize Status Item

        _statusItem = new BarStaticItem
        {
            Name = "barStaticItemProgressStatus",
            Caption = "就绪",
            Width = 1400,
            Alignment = BarItemLinkAlignment.Left
        };

        #endregion

        #region Initialize Progress Repository

        _progressRepository = new RepositoryItemProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            ShowTitle = true
        };

        _progressRepository.Appearance.ForeColor = System.Drawing.Color.White;
        _progressRepository.Appearance.BackColor = System.Drawing.Color.FromArgb(255, 114, 0);
        _progressRepository.Appearance.Options.UseBackColor = true;
        _progressRepository.Appearance.Options.UseForeColor = true;

        #endregion

        #region Initialize Progress Item

        _progressItem = new BarEditItem
        {
            Name = "barEditItemProgress",
            Width = 200,
            Edit = _progressRepository,
            EditValue = 0,
            Alignment = BarItemLinkAlignment.Right
        };

        #endregion

        #region Initialize Log Viewer Button

        _logViewerButton = new BarButtonItem
        {
            Name = "barButtonItemLogViewer",
            Caption = "详细日志",
            Width = 100,
            Alignment = BarItemLinkAlignment.Right
        };
        _logViewerButton.ItemClick += OnLogViewerButtonClick;

        #endregion

        #region Add Items to Bar Manager

        barManager.Items.Add(_statusItem);
        barManager.Items.Add(_progressItem);
        barManager.Items.Add(_logViewerButton);

        #endregion

        #region Setup Main Form Event Handlers

        _mainForm = template as RibbonForm;
        if (_mainForm != null)
        {
            _mainForm.Load += OnMainFormLoad;
        }

        #endregion
    }

    #endregion

    #region StatusBar Progress Methods

    public void SetProgress(int value, int maximum = 100)
    {
        if (_progressItem == null || _progressRepository == null)
        {
            return;
        }

        if (_mainForm != null && _mainForm.InvokeRequired)
        {
            _mainForm.Invoke(new Action(() => SetProgress(value, maximum)));
            return;
        }

        _progressRepository.Maximum = maximum;
        _progressItem.EditValue = value;
    }

    public int GetProgressMaximum()
    {
        return _progressRepository?.Maximum ?? 100;
    }

    public void SetProgressMaximum(int maximum)
    {
        if (_progressRepository == null)
        {
            return;
        }

        if (_mainForm != null && _mainForm.InvokeRequired)
        {
            _mainForm.Invoke(new Action(() => SetProgressMaximum(maximum)));
            return;
        }

        _progressRepository.Maximum = maximum;
    }

    public void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true)
    {
        if (_statusItem == null)
        {
            return;
        }

        if (_mainForm != null && _mainForm.InvokeRequired)
        {
            _mainForm.Invoke(new Action(() => SetStatusMessage(message, type, newline, log)));
            return;
        }

        _statusItem.Caption = message;

        #region Add Log to Log Viewer

        if (!log)
        {
            return;
        }

        if (_logViewerForm == null)
        {
            _logViewerForm = LogViewerForm.GetInstance();
        }

        if (_logViewerForm != null && !_logViewerForm.IsDisposed)
        {
            _logViewerForm.AddLog(message, type);
        }

        #endregion

        #region Write to File Log

        FileLoggerService.Instance.Log(message, type);

        #endregion
    }

    #region Convenience Methods for Different Log Types

    public void LogInfo(string message)
    {
        SetStatusMessage(message, MessageType.Info);
    }

    public void LogSuccess(string message)
    {
        SetStatusMessage(message, MessageType.Success);
    }

    public void LogWarning(string message)
    {
        SetStatusMessage(message, MessageType.Warning);
    }

    public void LogError(string message)
    {
        SetStatusMessage(message, MessageType.Error);
    }

    public void LogDebug(string message)
    {
        SetStatusMessage(message, MessageType.Debug);
    }

    public void LogTitle(string message)
    {
        SetStatusMessage(message, MessageType.Title);
    }

    #endregion

    public void ShowProgress()
    {
        if (_progressItem == null)
        {
            return;
        }

        if (_mainForm != null && _mainForm.InvokeRequired)
        {
            _mainForm.Invoke(new Action(() => ShowProgress()));
            return;
        }

        _progressItem.Visibility = BarItemVisibility.Always;
    }

    public void HideProgress()
    {
        if (_progressItem == null)
        {
            return;
        }

        if (_mainForm != null && _mainForm.InvokeRequired)
        {
            _mainForm.Invoke(new Action(() => HideProgress()));
            return;
        }

        _progressItem.Visibility = BarItemVisibility.Never;
    }

    public void ResetProgress()
    {
        SetProgress(0);
        SetStatusMessage("就绪");
    }

    #endregion

    #region Private Methods

    private void OnMainFormLoad(object sender, System.EventArgs e)
    {
        var ribbonControl = (_mainForm as RibbonForm)?.Ribbon;
        if (ribbonControl == null)
        {
            return;
        }

        var statusBar = ribbonControl.StatusBar;
        if (statusBar == null)
        {
            return;
        }

        statusBar.ItemLinks.Add(_statusItem);
        statusBar.ItemLinks.Add(_progressItem);
        statusBar.ItemLinks.Add(_logViewerButton);
    }

    private void OnLogViewerButtonClick(object sender, ItemClickEventArgs e)
    {
        _logViewerForm = LogViewerForm.GetInstance();
        if (_logViewerForm.Visible)
        {
            _logViewerForm.BringToFront();
        }
        else
        {
            _logViewerForm.Show();
        }
    }

    #endregion
}
