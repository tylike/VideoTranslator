using System.Text.RegularExpressions;

namespace SherpaJsonToSrt
{
    #region Sherpa ASR 配置

    public class SherpaAsrConfig
    {
        public string SherpaAsrPath { get; set; } = @"D:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\bin\sherpa-onnx-offline.exe";
        public string SenseVoiceModel { get; set; } = @"D:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-sense-voice-zh-en-ja-ko-yue-int8-2024-07-17\model.int8.onnx";
        public string SenseVoiceTokens { get; set; } = @"D:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-sense-voice-zh-en-ja-ko-yue-int8-2024-07-17\tokens.txt";
        public int NumThreads { get; set; } = 20;
        public bool UseItn { get; set; } = true;
    }

    #endregion

    #region Sherpa ASR 处理器

    public class SherpaAsrProcessor
    {
        private readonly SherpaAsrConfig _config;
        private readonly ProcessExecutor _executor;

        public SherpaAsrProcessor(SherpaAsrConfig? config = null)
        {
            _config = config ?? new SherpaAsrConfig();
            _executor = new ProcessExecutor();
        }

        #region 执行 Sherpa-ONNX 识别

        public CommandResult Process(string audioPath, string jsonOutputPath)
        {
            #region 构建命令参数

            var arguments = $"--sense-voice-model=\"{_config.SenseVoiceModel}\" " +
                          $"--sense-voice-use-itn={_config.UseItn.ToString().ToLower()} " +
                          $"--tokens=\"{_config.SenseVoiceTokens}\" " +
                          $"--num-threads={_config.NumThreads} " +
                          $"\"{audioPath}\"";

            #endregion

            #region 执行识别

            Console.WriteLine($"执行 Sherpa-ONNX 识别...");
            Console.WriteLine($"  音频文件: {audioPath}");
            Console.WriteLine($"  JSON输出: {jsonOutputPath}");
            Console.WriteLine($"  模型: {_config.SenseVoiceModel}");
            Console.WriteLine($"  线程数: {_config.NumThreads}");
            Console.WriteLine();

            var result = _executor.Execute(_config.SherpaAsrPath, arguments);

            #endregion

            #region 保存完整输出到文件

            if (result.Success)
            {
                var fullOutput = result.StandardOutput + result.StandardError;
                Console.WriteLine($"stdout 长度: {result.StandardOutput.Length} 字符");
                Console.WriteLine($"stderr 长度: {result.StandardError.Length} 字符");
                Console.WriteLine($"总输出长度: {fullOutput.Length} 字符");
                File.WriteAllText(jsonOutputPath, fullOutput, System.Text.Encoding.UTF8);
                Console.WriteLine($"Sherpa-ONNX 识别完成，输出已保存到: {jsonOutputPath}");
            }

            #endregion

            return result;
        }

        #endregion

        #region 异步执行 Sherpa-ONNX 识别

        public async Task<CommandResult> ProcessAsync(string audioPath, string jsonOutputPath)
        {
            return await Task.Run(() => Process(audioPath, jsonOutputPath));
        }

        #endregion

        #region 从完整输出中提取 JSON

        private string ExtractJsonFromOutput(string output)
        {
            #region 查找 JSON 起始和结束位置

            var startIndex = output.IndexOf("{\"lang\":");
            if (startIndex < 0)
            {
                return string.Empty;
            }

            var endIndex = output.LastIndexOf("}");
            if (endIndex < startIndex)
            {
                return string.Empty;
            }

            #endregion

            #region 提取 JSON 字符串

            var jsonString = output.Substring(startIndex, endIndex - startIndex + 1);

            #endregion

            return jsonString;
        }

        #endregion

        #region 转换 JSON 到 SRT

        public bool ConvertJsonToSrt(string jsonPath, string srtPath)
        {
            #region 检查文件是否存在

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"错误: JSON文件不存在: {jsonPath}");
                return false;
            }

            #endregion

            #region 读取并提取 JSON

            Console.WriteLine($"正在转换 JSON 到 SRT...");
            Console.WriteLine($"  JSON输入: {jsonPath}");
            Console.WriteLine($"  SRT输出: {srtPath}");

            var fullOutput = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
            var jsonContent = ExtractJsonFromOutput(fullOutput);

            if (string.IsNullOrEmpty(jsonContent))
            {
                Console.WriteLine("错误: 无法从输出中提取 JSON 数据");
                return false;
            }

            #endregion

            #region 解析 JSON

            var sherpaResult = SherpaJsonParser.Parse(jsonContent);

            if (sherpaResult.Timestamps.Count == 0 || sherpaResult.Tokens.Count == 0)
            {
                Console.WriteLine("警告: JSON中没有有效的时间戳或Token数据");
                return false;
            }

            #endregion

            #region 生成并写入 SRT

            var subtitles = SrtGenerator.GenerateSubtitles(sherpaResult);
            SrtWriter.Write(srtPath, subtitles);

            Console.WriteLine($"SRT转换完成，生成 {subtitles.Count} 条字幕");
            Console.WriteLine($"SRT文件已保存到: {srtPath}");

            #endregion

            return true;
        }

        #endregion

        #region 异步转换 JSON 到 SRT

        public async Task<bool> ConvertJsonToSrtAsync(string jsonPath, string srtPath)
        {
            return await Task.Run(() => ConvertJsonToSrt(jsonPath, srtPath));
        }

        #endregion
    }

    #endregion
}
