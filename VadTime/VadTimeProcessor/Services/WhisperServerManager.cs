using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using VadTimeProcessor.Models;
using VideoTranslator.Interfaces;

namespace VadTimeProcessor.Services;

/// <summary>
/// Whisper服务器管理器 - 负责管理Whisper服务端进程
/// </summary>
public class WhisperServerManager
{
    #region 私有字段

    private readonly string _executablePath;
    private readonly string _modelPath;
    private readonly string _host;
    private readonly int _port;
    private readonly bool _enableConsoleOutput;
    private Process? _serverProcess;
    private bool _isManagedByUs;
    private IProgressService? _progressService;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="executablePath">Whisper服务器可执行文件路径</param>
    /// <param name="modelPath">Whisper模型路径</param>
    /// <param name="serverUrl">服务器URL（如 http://127.0.0.1:8080）</param>
    /// <param name="enableConsoleOutput">是否启用控制台输出</param>
    public WhisperServerManager(
        string executablePath,
        string modelPath,
        string serverUrl,
        bool enableConsoleOutput = true)
    {
        _executablePath = executablePath ?? throw new ArgumentNullException(nameof(executablePath));
        _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
        _enableConsoleOutput = enableConsoleOutput;

        #region 解析服务器URL

        var uri = new Uri(serverUrl);
        _host = uri.Host;
        _port = uri.Port;

        #endregion
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置进度服务实例
    /// </summary>
    /// <param name="progressService">进度服务实例</param>
    public void SetProgressService(IProgressService progressService)
    {
        _progressService = progressService;
    }
    #endregion

    #region 公共属性

    /// <summary>
    /// 服务器是否正在运行
    /// </summary>
    public bool IsRunning => _serverProcess != null && !_serverProcess.HasExited;

    /// <summary>
    /// 服务器是否由本管理器启动
    /// </summary>
    public bool IsManagedByUs => _isManagedByUs;

    #endregion

    #region 公共方法

    /// <summary>
    /// 检查服务器是否可用
    /// </summary>
    /// <param name="timeoutMs">超时时间（毫秒），默认5000</param>
    /// <returns>服务器是否可用</returns>
    public async Task<bool> IsServerAvailableAsync(int timeoutMs = 5000)
    {
        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(_host, _port);
            var timeoutTask = Task.Delay(timeoutMs);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return false;
            }

            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查Whisper服务器进程是否运行
    /// </summary>
    /// <returns>进程是否运行</returns>
    public bool IsProcessRunning()
    {
        #region 检查是否有whisper-server进程

        var processes = Process.GetProcessesByName("whisper-server");
        return processes.Length > 0;

        #endregion
    }

    /// <summary>
    /// 确保服务器正在运行
    /// </summary>
    /// <returns>服务器是否可用</returns>
    public async Task<bool> EnsureServerRunningAsync()
    {
        #region 检查服务器是否可用

        if (await IsServerAvailableAsync())
        {
            _progressService?.Report("Whisper服务器已在运行");
            _isManagedByUs = false;
            return true;
        }

        #endregion

        #region 检查进程是否存在但不可用

        if (IsProcessRunning())
        {
            _progressService?.Warning("Whisper服务器进程存在但不可用，尝试停止...");
            StopServer();
        }

        #endregion

        #region 启动服务器

        _progressService?.Report("Whisper服务器未运行，正在启动...");
        var started = StartServer();

        if (!started)
        {
            _progressService?.Error("Whisper服务器启动失败");
            return false;
        }

        #endregion

        #region 等待服务器就绪

        _progressService?.Report("等待Whisper服务器就绪...");
        var maxRetries = 30;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            await Task.Delay(1000);
            retryCount++;

            if (await IsServerAvailableAsync())
            {
                _progressService?.Success($"Whisper服务器已就绪（耗时 {retryCount} 秒）");
                _isManagedByUs = true;
                return true;
            }
        }

        _progressService?.Error("Whisper服务器启动超时");
        return false;

        #endregion
    }

    /// <summary>
    /// 启动Whisper服务器
    /// </summary>
    /// <returns>是否启动成功</returns>
    public bool StartServer()
    {
        #region 验证文件

        if (!File.Exists(_executablePath))
        {
            _progressService?.Error($"Whisper服务器可执行文件不存在: {_executablePath}");
            return false;
        }

        if (!File.Exists(_modelPath))
        {
            _progressService?.Error($"Whisper模型文件不存在: {_modelPath}");
            return false;
        }

        #endregion

        #region 构建启动参数

        var arguments = $"-m \"{_modelPath}\" --port {_port} --host {_host} --print-realtime --print-progress";

        #endregion

        #region 启动进程

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _serverProcess = new Process { StartInfo = startInfo };
            _serverProcess.EnableRaisingEvents = true;

            #region 设置输出处理

            _serverProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    _progressService?.Report($"[Whisper Server] {e.Data}", MessageType.Debug);
                }
            };

            _serverProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    _progressService?.Report($"[Whisper Server] {e.Data}", MessageType.Error);
                }
            };

            #endregion

            #region 启动进程

            if (!_serverProcess.Start())
            {
                _progressService?.Error("启动Whisper服务器进程失败");
                return false;
            }

            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            _progressService?.Report($"Whisper服务器进程已启动 (PID: {_serverProcess.Id})");
            _progressService?.Report($"命令: {_executablePath} {arguments}");

            #endregion

            return true;
        }
        catch (Exception ex)
        {
            _progressService?.Error($"启动Whisper服务器失败: {ex.Message}");
            return false;
        }

        #endregion
    }

    /// <summary>
    /// 停止Whisper服务器
    /// </summary>
    public void StopServer()
    {
        #region 如果是我们启动的进程，停止它

        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _progressService?.Report($"正在停止Whisper服务器 (PID: {_serverProcess.Id})...");

            try
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(5000);
                _progressService?.Success("Whisper服务器已停止");
            }
            catch (Exception ex)
            {
                _progressService?.Error($"停止Whisper服务器失败: {ex.Message}");
            }
            finally
            {
                _serverProcess?.Dispose();
                _serverProcess = null;
            }

            return;
        }

        #endregion

        #region 如果不是我们启动的，检查是否有其他whisper-server进程

        if (IsProcessRunning())
        {
            _progressService?.Warning("检测到其他Whisper服务器进程，但不会停止（非本管理器启动）");
        }

        #endregion
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isManagedByUs)
        {
            StopServer();
        }

        _serverProcess?.Dispose();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 记录信息
    /// </summary>
    private void LogInfo(string message)
    {
        if (_enableConsoleOutput && _progressService == null)
        {
            Console.WriteLine(message);
        }
        else
        {
            _progressService?.Report(message);
        }
    }

    /// <summary>
    /// 记录警告
    /// </summary>
    private void LogWarning(string message)
    {
        if (_enableConsoleOutput && _progressService == null)
        {
            Console.WriteLine($"[警告] {message}");
        }
        else
        {
            _progressService?.Warning(message);
        }
    }

    /// <summary>
    /// 记录错误
    /// </summary>
    private void LogError(string message)
    {
        if (_enableConsoleOutput && _progressService == null)
        {
            Console.Error.WriteLine($"[错误] {message}");
        }
        else
        {
            _progressService?.Error(message);
        }
    }

    #endregion

    #region 静态工厂方法

    /// <summary>
    /// 从TranscribeOptions创建管理器
    /// </summary>
    /// <param name="options">转录选项</param>
    /// <returns>Whisper服务器管理器实例</returns>
    public static WhisperServerManager CreateFromOptions(TranscribeOptions options)
    {
        return new WhisperServerManager(
            options.Whisper.ServerExecutablePath,
            options.Whisper.ServerModelPath,
            options.Whisper.ServerUrl,
            options.Output.EnableConsoleOutput
        );
    }

    #endregion
}
