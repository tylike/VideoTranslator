using System.Diagnostics;
using System.Text;

namespace SherpaJsonToSrt
{
    #region 命令执行结果

    public class CommandResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }

    #endregion

    #region 命令执行器

    public class ProcessExecutor
    {
        private readonly string _workingDirectory;

        public ProcessExecutor(string? workingDirectory = null)
        {
            _workingDirectory = workingDirectory ?? string.Empty;
        }

        #region 执行命令

        public CommandResult Execute(string fileName, string arguments, bool captureOutput = true)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            #region 解析参数并添加到 ArgumentList

            var args = ParseArguments(arguments);
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            #endregion

            if (!string.IsNullOrEmpty(_workingDirectory))
            {
                startInfo.WorkingDirectory = _workingDirectory;
            }

            using var process = new Process { StartInfo = startInfo };
            
            #region 启动进程并实时读取输出

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    Console.Error.WriteLine(e.Data);
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            #endregion

            return new CommandResult
            {
                Success = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString(),
                StandardError = errorBuilder.ToString()
            };
        }

        #endregion

        #region 异步执行命令

        public async Task<CommandResult> ExecuteAsync(string fileName, string arguments, bool captureOutput = true)
        {
            return await Task.Run(() => Execute(fileName, arguments, captureOutput));
        }

        #endregion

        #region 解析命令行参数

        private List<string> ParseArguments(string arguments)
        {
            var result = new List<string>();
            var currentArg = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < arguments.Length; i++)
            {
                char c = arguments[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < arguments.Length && arguments[i + 1] == '"')
                    {
                        currentArg.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (currentArg.Length > 0)
                    {
                        result.Add(currentArg.ToString());
                        currentArg.Clear();
                    }
                }
                else
                {
                    currentArg.Append(c);
                }
            }

            if (currentArg.Length > 0)
            {
                result.Add(currentArg.ToString());
            }

            return result;
        }

        #endregion
    }

    #endregion
}
