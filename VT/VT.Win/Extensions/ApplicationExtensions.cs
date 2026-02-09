using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.XtraBars;
using DevExpress.XtraEditors.Repository;
using System;
using System.Windows.Forms;

namespace VT.Win.Extensions
{
    public static class ApplicationExtensions
    {
        #region Private Helper Methods

        private static VTWindowsFormsApplication GetWinApplication(this XafApplication application)
        {
            return application as VTWindowsFormsApplication;
        }

        private static BarStaticItem GetStatusItem(this XafApplication application)
        {
            var winApp = application.GetWinApplication();
            if (winApp == null)
            {
                return null;
            }

            var statusItemField = winApp.GetType().GetField("_statusItem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return statusItemField?.GetValue(winApp) as BarStaticItem;
        }

        private static BarEditItem GetProgressItem(this XafApplication application)
        {
            var winApp = application.GetWinApplication();
            if (winApp == null)
            {
                return null;
            }

            var progressItemField = winApp.GetType().GetField("_progressItem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return progressItemField?.GetValue(winApp) as BarEditItem;
        }

        private static RepositoryItemProgressBar GetProgressRepository(this XafApplication application)
        {
            var winApp = application.GetWinApplication();
            if (winApp == null)
            {
                return null;
            }

            var progressRepositoryField = winApp.GetType().GetField("_progressRepository", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return progressRepositoryField?.GetValue(winApp) as RepositoryItemProgressBar;
        }

        private static Form GetMainForm(this XafApplication application)
        {
            var winApp = application.GetWinApplication();
            if (winApp == null)
            {
                return null;
            }

            var mainFormField = winApp.GetType().GetField("_mainForm", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            return mainFormField?.GetValue(winApp) as Form;
        }

        #endregion

        #region Public Extension Methods

        public static void SetProgress(this XafApplication application, int value, int maximum = 100)
        {
            var progressItem = application.GetProgressItem();
            var progressRepository = application.GetProgressRepository();
            var mainForm = application.GetMainForm();

            if (progressItem == null || progressRepository == null)
            {
                return;
            }

            if (mainForm != null && mainForm.InvokeRequired)
            {
                mainForm.Invoke(new Action(() => application.SetProgress(value, maximum)));
                return;
            }

            progressRepository.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Solid;
            progressRepository.Maximum = maximum;
            progressItem.EditValue = value;
        }

        public static void SetStatusMessage(this XafApplication application, string message)
        {
            var statusItem = application.GetStatusItem();
            var mainForm = application.GetMainForm();

            if (statusItem == null)
            {
                return;
            }

            if (mainForm != null && mainForm.InvokeRequired)
            {
                mainForm.Invoke(new Action(() => application.SetStatusMessage(message)));
                return;
            }

            statusItem.Caption = message;
        }

        public static void ShowProgress(this XafApplication application, bool marquee = false)
        {
            var progressItem = application.GetProgressItem();
            var progressRepository = application.GetProgressRepository();
            var mainForm = application.GetMainForm();

            if (progressItem == null)
            {
                return;
            }

            if (mainForm != null && mainForm.InvokeRequired)
            {
                mainForm.Invoke(new Action(() => application.ShowProgress(marquee)));
                return;
            }

            if (marquee && progressRepository != null)
            {
                progressRepository.ProgressViewStyle = DevExpress.XtraEditors.Controls.ProgressViewStyle.Solid;
                progressItem.EditValue = 50;
            }

            progressItem.Visibility = BarItemVisibility.Always;
        }

        public static void HideProgress(this XafApplication application)
        {
            var progressItem = application.GetProgressItem();
            var mainForm = application.GetMainForm();

            if (progressItem == null)
            {
                return;
            }

            if (mainForm != null && mainForm.InvokeRequired)
            {
                mainForm.Invoke(new Action(() => application.HideProgress()));
                return;
            }

            progressItem.Visibility = BarItemVisibility.Never;
        }

        public static void ResetProgress(this XafApplication application)
        {
            application.SetProgress(0);
            application.SetStatusMessage("就绪");
        }

        #endregion
    }
}
