using System;
using System.Windows.Forms;
using Common.Logging;

namespace SimpleVideoPlayer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Common.Logging.LoggerService.Initialize("SimpleVideoPlayer");
            var logger = Common.Logging.LoggerService.ForContext("Program");
            logger.Information("应用程序启动");
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            
            logger.Information("应用程序退出");
            Common.Logging.LoggerService.Close();
        }
    }
}
