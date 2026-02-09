using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AudioSample.Utils
{
    public static class AudioUtils
    {
        #region 音频格式转换

        public static async Task<string> ConvertToWavAsync(string inputPath, string outputPath, int sampleRate = 16000, int channels = 1)
        {
            var ffmpegPath = FindFFmpeg();
            if (ffmpegPath == null)
            {
                throw new FileNotFoundException("FFmpeg not found in PATH");
            }

            var arguments = $"-i \"{inputPath}\" -ar {sampleRate} -ac {channels} \"{outputPath}\"";
            var result = await RunProcessAsync(ffmpegPath, arguments);

            if (result.ExitCode != 0)
            {
                throw new Exception($"FFmpeg conversion failed: {result.Error}");
            }

            return outputPath;
        }

        #endregion

        #region 进程执行

        public static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, int timeoutMs = 300000)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            using var process = new Process { StartInfo = processInfo };
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (s, e) => outputBuilder.AppendLine(e.Data);
            process.ErrorDataReceived += (s, e) => errorBuilder.AppendLine(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completedTask = await Task.WhenAny(
                Task.Run(() => process.WaitForExit()),
                Task.Delay(timeoutMs)
            );

            if (completedTask == Task.Delay(timeoutMs))
            {
                process.Kill();
                throw new TimeoutException($"Process timed out after {timeoutMs}ms");
            }

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString()
            };
        }

        #endregion

        #region 工具方法

        private static string? FindFFmpeg()
        {
            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
            var exeNames = new[] { "ffmpeg.exe", "ffmpeg" };

            foreach (var path in paths)
            {
                foreach (var exeName in exeNames)
                {
                    var fullPath = Path.Combine(path, exeName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            return null;
        }

        public static double ParseTimeToSeconds(string timeString)
        {
            var match = Regex.Match(timeString, @"(\d+):(\d+):(\d+)[.,](\d+)");
            if (match.Success)
            {
                var hours = int.Parse(match.Groups[1].Value);
                var minutes = int.Parse(match.Groups[2].Value);
                var seconds = int.Parse(match.Groups[3].Value);
                var milliseconds = int.Parse(match.Groups[4].Value.PadRight(3, '0').Substring(0, 3));
                return hours * 3600 + minutes * 60 + seconds + milliseconds / 1000.0;
            }

            match = Regex.Match(timeString, @"(\d+):(\d+)[.,](\d+)");
            if (match.Success)
            {
                var minutes = int.Parse(match.Groups[1].Value);
                var seconds = int.Parse(match.Groups[2].Value);
                var milliseconds = int.Parse(match.Groups[3].Value.PadRight(3, '0').Substring(0, 3));
                return minutes * 60 + seconds + milliseconds / 1000.0;
            }

            match = Regex.Match(timeString, @"(\d+)[.,](\d+)");
            if (match.Success)
            {
                var seconds = int.Parse(match.Groups[1].Value);
                var milliseconds = int.Parse(match.Groups[2].Value.PadRight(3, '0').Substring(0, 3));
                return seconds + milliseconds / 1000.0;
            }

            return 0;
        }

        public static string FormatTime(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds:000}";
            }
            return $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds:000}";
        }

        #endregion
    }

    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
