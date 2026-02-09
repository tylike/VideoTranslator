using PublishToBilibili.Interfaces;
using PublishToBilibili.Models;
using FlaUI.Core.Definitions;

namespace PublishToBilibili.Services
{
    public class BilibiliPublishApi
    {
        private readonly IProcessService _processService;
        private readonly IWindowService _windowService;
        private const string BcutPath = @"C:\Users\Administrator\AppData\Local\BcutBilibili\BCUT.exe";
        private const string PublishButtonName = "发布本地作品";

        public BilibiliPublishApi(IProcessService processService, IWindowService windowService)
        {
            _processService = processService;
            _windowService = windowService;
        }

        public bool PublishVideo(PublishInfo publishInfo)
        {
            try
            {
                Console.WriteLine("=== Starting Bilibili Publish Process ===");

                if (!EnsureBcutRunning())
                {
                    Console.WriteLine("Failed to start BCUT.", MessageType.Error);
                    return false;
                }

                if (!ClickPublishButton())
                {
                    Console.WriteLine("Failed to click publish button.", MessageType.Error);
                    return false;
                }

                if (!SelectVideoFile(publishInfo.VideoFilePath))
                {
                    Console.WriteLine("Failed to select video file.", MessageType.Error);
                    return false;
                }

                if (!FillPublishForm(publishInfo))
                {
                    Console.WriteLine("Failed to fill publish form.", MessageType.Error);
                    return false;
                }

                Console.WriteLine("=== Publish Process Completed Successfully ===");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in publish process: {ex.Message}");
                return false;
            }
        }

        private bool EnsureBcutRunning()
        {
            Console.WriteLine("Checking BCUT status...");
            
            var processInfo = _processService.GetProcessByPath(BcutPath);
            
            if (processInfo == null)
            {
                processInfo = _processService.StartProcess(BcutPath);
            }

            if (processInfo == null)
            {
                Console.WriteLine("Failed to start BCUT.");
                return false;
            }

            Console.WriteLine($"BCUT is running. Process ID: {processInfo.Id}");
            return true;
        }

        private bool ClickPublishButton()
        {
            Console.WriteLine("Searching for '发布本地作品' button...");
            
            var processInfo = _processService.GetProcessByPath(BcutPath);
            if (processInfo == null || processInfo.MainWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            var publishButton = _windowService.FindButton(processInfo.MainWindowHandle, PublishButtonName);
            if (publishButton == null)
            {
                return false;
            }

            Console.WriteLine($"Found publish button: {publishButton.Name}");
            _windowService.ActivateWindow(processInfo.MainWindowHandle);
            System.Threading.Thread.Sleep(500);

            return _windowService.ClickButton(publishButton);
        }

        private bool SelectVideoFile(string filePath)
        {
            Console.WriteLine($"Selecting video file: {filePath}");
            System.Threading.Thread.Sleep(1000);
            
            var processInfo = _processService.GetProcessByPath(BcutPath);
            if (processInfo == null || processInfo.MainWindowHandle == IntPtr.Zero)
            {
                return false;
            }
            
            var result = _windowService.SelectFileInDialog(filePath, processInfo.MainWindowHandle);
            
            if (result)
            {
                Console.WriteLine("File selected successfully, waiting for publish window to open...", MessageType.Info);
                System.Threading.Thread.Sleep(500);
            }
            
            return result;
        }

        private bool FillPublishForm(PublishInfo publishInfo)
        {
            try
            {
                Console.WriteLine("Filling publish form...", MessageType.Info);
                
                var publishWindowHandle = IntPtr.Zero;
                int maxRetries = 5;
                int retryDelay = 2000;

                for (int i = 0; i < maxRetries; i++)
                {
                    Console.WriteLine($"Attempt {i + 1}/{maxRetries} to find publish window...", MessageType.Info);
                    System.Threading.Thread.Sleep(retryDelay);
                    
                    publishWindowHandle = FindPublishWindowHandle();
                    if (publishWindowHandle != IntPtr.Zero)
                    {
                        Console.WriteLine($"Publish window found on attempt {i + 1}", MessageType.Success);
                        break;
                    }
                }

                if (publishWindowHandle == IntPtr.Zero)
                {
                    Console.WriteLine("Publish window not found after all retries.", MessageType.Error);
                    return false;
                }

                var formService = new PublishFormService();
                return formService.FillPublishFormWithInfo(publishWindowHandle, publishInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error filling form: {ex.Message}", MessageType.Error);
                return false;
            }
        }

        private IntPtr FindPublishWindowHandle()
        {
            try
            {
                Console.WriteLine("Searching for publish window...");
                
                var automation = new FlaUI.UIA3.UIA3Automation();
                var windows = automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window));
                
                Console.WriteLine($"Found {windows.Length} windows on desktop:");
                
                foreach (var window in windows)
                {
                    Console.WriteLine($"  - Window Name: '{window.Name}', ClassName: '{window.ClassName}'");
                }
                
                foreach (var window in windows)
                {
                    if (window.Name.Contains("必剪"))
                    {
                        Console.WriteLine($"\nFound window with '必剪' in name: {window.Name}, ClassName: {window.ClassName}", MessageType.Info);
                        
                        if (window.ClassName.Contains("BExportReleaseDialog"))
                        {
                            Console.WriteLine($"Found publish window: {window.Name}, ClassName: {window.ClassName}", MessageType.Success);
                            return window.Properties.NativeWindowHandle.Value;
                        }
                        else
                        {
                            Console.WriteLine($"Window found but ClassName doesn't match 'BExportReleaseDialog'", MessageType.Warning);
                        }
                    }
                }

                Console.WriteLine("\nSearching for modal dialog in BCUT window...", MessageType.Info);
                var processInfo = _processService.GetProcessByPath(BcutPath);
                if (processInfo != null && processInfo.MainWindowHandle != IntPtr.Zero)
                {
                    Console.WriteLine($"Process ID: {processInfo.Id}, MainWindowHandle: {processInfo.MainWindowHandle}", MessageType.Info);
                    
                    var allWindowsByProcess = automation.GetDesktop().FindAllChildren(cf => cf.ByProcessId(processInfo.Id));
                    Console.WriteLine($"Found {allWindowsByProcess.Length} windows for process {processInfo.Id}:", MessageType.Info);
                    
                    foreach (var procWindow in allWindowsByProcess)
                    {
                        Console.WriteLine($"  - Window Name: '{procWindow.Name}', ClassName: '{procWindow.ClassName}', Handle: {procWindow.Properties.NativeWindowHandle.Value}", MessageType.Info);
                        
                        if (procWindow.Name.Contains("必剪") && procWindow.ClassName.Contains("BExportReleaseDialog"))
                        {
                            Console.WriteLine($"Found publish window directly: {procWindow.Name}, ClassName: {procWindow.ClassName}", MessageType.Success);
                            return procWindow.Properties.NativeWindowHandle.Value;
                        }
                    }
                    
                    Console.WriteLine("Publish window not found directly, searching in each window...", MessageType.Info);
                    
                    foreach (var procWindow in allWindowsByProcess)
                    {
                        Console.WriteLine($"Searching in window: {procWindow.Name}, ClassName: {procWindow.ClassName}", MessageType.Info);
                        
                        var modalWindows = procWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.Window));
                        Console.WriteLine($"Found {modalWindows.Length} modal windows in this window:", MessageType.Info);
                        
                        foreach (var modal in modalWindows)
                        {
                            Console.WriteLine($"  - Modal Window Name: '{modal.Name}', ClassName: '{modal.ClassName}', Handle: {modal.Properties.NativeWindowHandle.Value}", MessageType.Info);
                            
                            if (modal.Name.Contains("必剪") || modal.ClassName.Contains("BExportReleaseDialog"))
                            {
                                Console.WriteLine($"Found publish modal window: {modal.Name}, ClassName: {modal.ClassName}", MessageType.Success);
                                return modal.Properties.NativeWindowHandle.Value;
                            }
                        }
                    }
                }

                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding publish window: {ex.Message}");
                return IntPtr.Zero;
            }
        }
    }
}
