using System;
using System.Windows;
using Common.Logging;

namespace VideoPlayer
{
    public partial class App : Application
    {
        #region 应用程序启动和关闭

        protected override void OnStartup(StartupEventArgs e)
        {
            Common.Logging.LoggerService.Initialize("VideoPlayer");
            var logger = Common.Logging.LoggerService.ForContext<App>();
            logger.Information("应用程序启动");
            
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var logger = Common.Logging.LoggerService.ForContext<App>();
            logger.Information("应用程序退出开始");
            
            try
            {
                foreach (Window window in this.Windows)
                {
                    if (window is MainWindow mainWindow)
                    {
                        mainWindow.Close();
                    }
                }
                
                System.Threading.Thread.Sleep(500);
                
                Common.Logging.LoggerService.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用程序退出时发生异常: {ex.Message}");
            }
            
            logger.Information("应用程序退出完成");
            base.OnExit(e);
        }

        #endregion
    }
}