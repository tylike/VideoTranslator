using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.ApplicationBuilder;
using DevExpress.ExpressApp.Win.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.XtraEditors;
using LibVLCSharp.Shared;
using System.IO;
using System.Reflection;
using VideoTranslator.Config;
using Console = global::System.Console;
namespace VT.Win
{
    internal static class Program
    {
        private const string OutputFilePath = @"d:\VideoTranslator\build_output.txt";

        private class DualWriter : TextWriter
        {
            private readonly TextWriter _consoleWriter;
            private readonly StreamWriter _fileWriter;

            public DualWriter(TextWriter consoleWriter, string filePath)
            {
                _consoleWriter = consoleWriter;
                _fileWriter = new StreamWriter(filePath, false, System.Text.Encoding.UTF8) { AutoFlush = true };
            }

            public override void Write(char value)
            {
                _consoleWriter.Write(value);
                _fileWriter.Write(value);
            }

            public override void Write(string value)
            {
                _consoleWriter.Write(value);
                _fileWriter.Write(value);
            }

            public override void WriteLine(string value)
            {
                _consoleWriter.WriteLine(value);
                _fileWriter.WriteLine(value);
            }

            public override void WriteLine()
            {
                _consoleWriter.WriteLine();
                _fileWriter.WriteLine();
            }

            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _fileWriter?.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        static bool ContainsArgument(string[] args, string argument)
        {
            return args.Any(arg => arg.TrimStart('/').TrimStart('-').ToLower() == argument.ToLower());
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            bool consoleAllocated = ConsoleAllocator.ShowConsole(args);

            if (consoleAllocated)
            {
                System.Console.SetOut(new DualWriter(System.Console.Out, OutputFilePath));
            }

            if (ContainsArgument(args, "help") || ContainsArgument(args, "h"))
            {
                System.Console.WriteLine("Usage: VT.Win.exe [options]");
                System.Console.WriteLine();
                System.Console.WriteLine("Options:");
                    System.Console.WriteLine("  --help, -h       Show this help message");
                System.Console.WriteLine("  --noconsole, -nc Hide console window");
                System.Console.WriteLine("  --updateDatabase Update database schema");
                System.Console.WriteLine("  --forceUpdate    Force database update");
                System.Console.WriteLine("  --silent         Silent database update");
                System.Console.WriteLine();
                System.Console.WriteLine("Note: Console window is shown by default for debugging.");
                System.Console.WriteLine();
                System.Console.WriteLine($"Exit codes: 0 - {DBUpdaterStatus.UpdateCompleted}");
                System.Console.WriteLine($"            1 - {DBUpdaterStatus.UpdateError}");
                System.Console.WriteLine($"            2 - {DBUpdaterStatus.UpdateNotNeeded}");
                if (consoleAllocated)
                {
                    System.Console.WriteLine();
                    System. Console.WriteLine("按任意键退出...");
                    System.Console.ReadKey(true);
                }
                return 0;
            }
            DevExpress.ExpressApp.FrameworkSettings.DefaultSettingsCompatibilityMode = DevExpress.ExpressApp.FrameworkSettingsCompatibilityMode.Latest;
#if EASYTEST
            DevExpress.ExpressApp.Win.EasyTest.EasyTestRemotingRegistration.Register();
#endif
            WindowsFormsSettings.LoadApplicationSettings();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LibVLCSharp.Shared.Core.Initialize();
            DevExpress.Utils.ToolTipController.DefaultController.ToolTipType = DevExpress.Utils.ToolTipType.SuperTip;
            if (Tracing.GetFileLocationFromSettings() == DevExpress.Persistent.Base.FileLocation.CurrentUserApplicationDataFolder)
            {
                Tracing.LocalUserAppDataPath = Application.LocalUserAppDataPath;
            }
            Tracing.Initialize();

            string connectionString = ConfigurationService.Configuration.Database.ConnectionString;
            ArgumentNullException.ThrowIfNull(connectionString);
            var winApplication = ApplicationBuilder.BuildApplication(connectionString);

            if (ContainsArgument(args, "updateDatabase"))
            {
                using var dbUpdater = new WinDBUpdater(() => winApplication);
                return dbUpdater.Update(
                    forceUpdate: ContainsArgument(args, "forceUpdate"),
                    silent: ContainsArgument(args, "silent"));
            }

            try
            {
                winApplication.Setup();
                winApplication.Start();
            }
            catch (Exception e)
            {
                winApplication.StopSplash();
                winApplication.HandleException(e);
            }
            return 0;
        }
    }
}
