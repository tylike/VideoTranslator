using DevExpress.ExpressApp;
using DevExpress.XtraBars;
using DevExpress.XtraEditors.Repository;
using System;
using System.Windows.Forms;
using VideoTranslator.Interfaces;
using VT.Win.Forms;

namespace VT.Win.Services;
using MessageType = VideoTranslator.Interfaces.MessageType; 

public class WinProgressService : IProgressService
{
    #region Private Fields

    private readonly RibbonTemplateCustomizer _ribbonTemplateCustomizer;

    #endregion

    #region Constructor

    public WinProgressService(RibbonTemplateCustomizer ribbonTemplateCustomizer)
    {
        _ribbonTemplateCustomizer = ribbonTemplateCustomizer ?? throw new ArgumentNullException(nameof(ribbonTemplateCustomizer));
    }

    #endregion

    #region Public Properties

    public object Application { get; set; }

    #endregion

    #region Public Methods

    public void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true)
    {
        _ribbonTemplateCustomizer.SetStatusMessage(message, type, newline, log);
    }

    #region Convenience Methods for Different Log Types

    public void LogInfo(string message)
    {
        _ribbonTemplateCustomizer.LogInfo(message);
    }

    public void LogSuccess(string message)
    {
        _ribbonTemplateCustomizer.LogSuccess(message);
    }

    public void LogWarning(string message)
    {
        _ribbonTemplateCustomizer.LogWarning(message);
    }

    public void LogError(string message)
    {
        _ribbonTemplateCustomizer.LogError(message);
    }

    public void LogDebug(string message)
    {
        _ribbonTemplateCustomizer.LogDebug(message);
    }

    public void LogTitle(string message)
    {
        _ribbonTemplateCustomizer.LogTitle(message);
    }

    #endregion

    public void ShowProgress(bool marquee = false)
    {
        _ribbonTemplateCustomizer.ShowProgress();
    }

    public void HideProgress()
    {
        _ribbonTemplateCustomizer.HideProgress();
    }

    public void ResetProgress()
    {
        _ribbonTemplateCustomizer.ResetProgress();
    }

    public void ReportProgress(double value)
    {
        _ribbonTemplateCustomizer.SetProgress((int)value, _ribbonTemplateCustomizer.GetProgressMaximum());
    }

    public void SetProgressMaxValue(double value)
    {
        _ribbonTemplateCustomizer.SetProgressMaximum((int)value);
    }

    #endregion
}
