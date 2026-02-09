using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Updating;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Controls;
using DevExpress.ExpressApp.Win.Templates.Ribbon;
using DevExpress.ExpressApp.Win.Utils;
using DevExpress.ExpressApp.Xpo;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraReports.Templates;
using System.ComponentModel;
using VT.Win.Services;
using VT.Win.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace VT.Win;
using MessageType = VideoTranslator.Interfaces.MessageType;

// For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Win.WinApplication._members
public class VTWindowsFormsApplication : WinApplication
{
    #region Private Fields

    private RibbonTemplateCustomizer _ribbonTemplateCustomizer;

    #endregion

    public VTWindowsFormsApplication()
    {
        SplashScreen = new DXSplashScreen(typeof(XafSplashScreen), new DefaultOverlayFormOptions());
        ApplicationName = "VT";
        CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
        UseOldTemplates = false;
        DatabaseVersionMismatch += VTWindowsFormsApplication_DatabaseVersionMismatch;
        CustomizeLanguagesList += VTWindowsFormsApplication_CustomizeLanguagesList;            
        this.CustomHandleException += VTWindowsFormsApplication_CustomHandleException;
    }

    protected override void OnCustomizeTemplate(IFrameTemplate frameTemplate, string templateContextName)
    {
        if (_ribbonTemplateCustomizer == null)
        {
            _ribbonTemplateCustomizer = ServiceProvider?.GetService(typeof(RibbonTemplateCustomizer)) as RibbonTemplateCustomizer;
        }

        _ribbonTemplateCustomizer?.CustomizeTemplate(frameTemplate);
        base.OnCustomizeTemplate(frameTemplate, templateContextName);
    }

    protected override void HandleExceptionCore(Exception e)
    {
        base.HandleExceptionCore(e);
    }

    private void VTWindowsFormsApplication_CustomHandleException(object sender, CustomHandleExceptionEventArgs e)
    {
        if (e.Exception is UserFriendlyException)
        {
            e.Handled = false;
        }
    }

    void VTWindowsFormsApplication_CustomizeLanguagesList(object sender, CustomizeLanguagesListEventArgs e)
    {
        string userLanguageName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
        if (userLanguageName != "en-US" && e.Languages.IndexOf(userLanguageName) == -1)
        {
            e.Languages.Add(userLanguageName);
        }
    }
    void VTWindowsFormsApplication_DatabaseVersionMismatch(object sender, DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs e)
    {
#if EASYTEST
        e.Updater.Update();
        e.Handled = true;
#else
        if (System.Diagnostics.Debugger.IsAttached)
        {
            e.Updater.Update();
            e.Handled = true;
        }
        else
        {
            string message = "The application cannot connect to the specified database, " +
                "because the database doesn't exist, its version is older " +
                "than that of the application or its schema does not match " +
                "the ORM data model structure. To avoid this error, use one " +
                "of the solutions from the https://www.devexpress.com/kb=T367835 KB Article.";

            if (e.CompatibilityError != null && e.CompatibilityError.Exception != null)
            {
                message += "\r\n\r\nInner exception: " + e.CompatibilityError.Exception.Message;
            }
            throw new InvalidOperationException(message);
        }
#endif
    }

    #region StatusBar Progress Methods

    public void SetProgress(int value, int maximum = 100)
    {
        _ribbonTemplateCustomizer.SetProgress(value, maximum);
    }

    public void SetStatusMessage(string message)
    {
        _ribbonTemplateCustomizer.SetStatusMessage(message);
    }

    public void SetStatusMessage(string message, MessageType type, bool newline = true, bool log = true)
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

    public void ShowProgress()
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

    #endregion
}
