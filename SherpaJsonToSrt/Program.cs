using System.Text.Json;
using System.Linq;

namespace SherpaJsonToSrt
{
    #region 数据模型

    public class SherpaResult
    {
        public string Lang { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<double> Timestamps { get; set; } = new();
        public List<string> Tokens { get; set; } = new();
    }

    public class SrtSubtitle
    {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    #endregion

    #region 服务类

    public class SherpaJsonParser
    {
        public static SherpaResult Parse(string jsonContent)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<SherpaResult>(jsonContent, options) ?? new SherpaResult();
        }
    }

    public class SrtGenerator
    {
        public static List<SrtSubtitle> GenerateSubtitles(SherpaResult sherpaResult)
        {
            var subtitles = new List<SrtSubtitle>();
            var timestamps = sherpaResult.Timestamps;
            var tokens = sherpaResult.Tokens;

            if (timestamps.Count == 0 || tokens.Count == 0)
            {
                return subtitles;
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var startTime = TimeSpan.FromSeconds(timestamps[i]);
                var endTime = TimeSpan.FromSeconds(timestamps[Math.Min(i + 1, timestamps.Count - 1)]);

                subtitles.Add(new SrtSubtitle
                {
                    Index = subtitles.Count + 1,
                    StartTime = startTime,
                    EndTime = endTime,
                    Text = token
                });
            }

            return subtitles;
        }
    }

    public class SrtWriter
    {
        public static void Write(string outputPath, List<SrtSubtitle> subtitles)
        {
            using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

            foreach (var subtitle in subtitles)
            {
                writer.WriteLine(subtitle.Index);
                writer.WriteLine($"{FormatTime(subtitle.StartTime)} --> {FormatTime(subtitle.EndTime)}");
                writer.WriteLine(subtitle.Text);
                writer.WriteLine();
            }
        }

        public static string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2},{time.Milliseconds:D3}";
        }
    }

    #endregion

    class Program
    {
        static async Task Main(string[] args)
        {
            #region 参数处理

            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            string mode = args[0].ToLower();

            #endregion

            #region 批处理模式

            if (mode == "batch" || mode == "-b")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("错误: 批处理模式需要指定音频文件路径");
                    Console.WriteLine("用法: SherpaJsonToSrt batch <音频文件路径> [--thresholds 0.5,0.8,0.9]");
                    return;
                }

                var audioPath = args[1];

                #region 解析阈值参数

                var config = new BatchProcessConfig();
                var thresholdsIndex = Array.IndexOf(args, "--thresholds");
                if (thresholdsIndex >= 0 && thresholdsIndex + 1 < args.Length)
                {
                    try
                    {
                        var thresholdsStr = args[thresholdsIndex + 1];
                        var thresholds = thresholdsStr.Split(',').Select(t => double.Parse(t.Trim())).ToList();
                        config.SpeakerClusterThresholds = thresholds;
                        Console.WriteLine($"使用自定义阈值: {string.Join(", ", thresholds)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"警告: 无法解析阈值参数，使用默认值: {string.Join(", ", config.SpeakerClusterThresholds)}");
                        Console.WriteLine($"错误信息: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"使用默认阈值: {string.Join(", ", config.SpeakerClusterThresholds)}");
                }

                #endregion

                var processor = new AudioBatchProcessor(config);
                await processor.ProcessAsync(audioPath);
                return;
            }

            #endregion

            #region JSON 转 SRT 模式

            if (mode == "convert" || mode == "-c")
            {
                string jsonPath;
                string srtPath;

                if (args.Length >= 3)
                {
                    jsonPath = args[1];
                    srtPath = args[2];
                }
                else
                {
                    Console.WriteLine("错误: 转换模式需要指定 JSON 和 SRT 文件路径");
                    Console.WriteLine("用法: SherpaJsonToSrt convert <JSON路径> <SRT路径>");
                    return;
                }

                await ConvertJsonToSrt(jsonPath, srtPath);
                return;
            }

            #endregion

            #region 兼容旧版本（直接指定 JSON 和 SRT 路径）

            if (args.Length >= 2 && File.Exists(args[0]))
            {
                await ConvertJsonToSrt(args[0], args[1]);
                return;
            }

            #endregion

            PrintUsage();
        }

        #region JSON 转 SRT

        static async Task ConvertJsonToSrt(string jsonPath, string srtPath)
        {
            #region 参数验证

            if (!File.Exists(jsonPath))
            {
                Console.WriteLine($"错误: JSON文件不存在: {jsonPath}");
                return;
            }

            #endregion

            #region 读取JSON

            Console.WriteLine("正在读取JSON文件...");
            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            Console.WriteLine($"JSON文件大小: {jsonContent.Length} 字符");
            Console.WriteLine();

            #endregion

            #region 解析JSON

            Console.WriteLine("正在解析JSON...");
            var sherpaResult = SherpaJsonParser.Parse(jsonContent);
            Console.WriteLine($"识别语言: {sherpaResult.Lang}");
            Console.WriteLine($"识别文本长度: {sherpaResult.Text.Length} 字符");
            Console.WriteLine($"时间戳数量: {sherpaResult.Timestamps.Count}");
            Console.WriteLine($"Token数量: {sherpaResult.Tokens.Count}");
            Console.WriteLine();

            #endregion

            #region 生成SRT

            Console.WriteLine("正在生成SRT字幕...");
            var subtitles = SrtGenerator.GenerateSubtitles(sherpaResult);
            Console.WriteLine($"生成字幕数量: {subtitles.Count}");
            Console.WriteLine();

            #endregion

            #region 写入SRT文件

            Console.WriteLine($"正在写入SRT文件: {srtPath}");
            SrtWriter.Write(srtPath, subtitles);
            Console.WriteLine("SRT文件写入完成");
            Console.WriteLine();

            #endregion

            #region 显示预览

            PrintPreview(subtitles);

            #endregion

            Console.WriteLine();
            Console.WriteLine("处理完成！");
        }

        #endregion

        #region 显示预览

        static void PrintPreview(List<SrtSubtitle> subtitles)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  SRT字幕预览 (前10条)");
            Console.WriteLine("========================================");
            var previewCount = Math.Min(10, subtitles.Count);
            for (int i = 0; i < previewCount; i++)
            {
                var sub = subtitles[i];
                Console.WriteLine($"{sub.Index}");
                Console.WriteLine($"{SrtWriter.FormatTime(sub.StartTime)} --> {SrtWriter.FormatTime(sub.EndTime)}");
                Console.WriteLine(sub.Text);
                Console.WriteLine();
            }

            if (subtitles.Count > 10)
            {
                Console.WriteLine($"... 还有 {subtitles.Count - 10} 条字幕");
            }
        }

        #endregion

        #region 打印使用说明

        static void PrintUsage()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  SherpaJsonToSrt - 音频识别工具");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("使用方法:");
            Console.WriteLine();
            Console.WriteLine("  批处理模式（完整音频识别）:");
            Console.WriteLine("    SherpaJsonToSrt batch <音频文件路径>");
            Console.WriteLine("    SherpaJsonToSrt -b <音频文件路径>");
            Console.WriteLine();
            Console.WriteLine("  JSON 转 SRT 模式:");
            Console.WriteLine("    SherpaJsonToSrt convert <JSON路径> <SRT路径>");
            Console.WriteLine("    SherpaJsonToSrt -c <JSON路径> <SRT路径>");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  SherpaJsonToSrt batch D:\\audio\\test.mp3");
            Console.WriteLine("  SherpaJsonToSrt convert input.json output.srt");
            Console.WriteLine();
            Console.WriteLine("功能说明:");
            Console.WriteLine("  - Whisper 词级别 SRT 生成");
            Console.WriteLine("  - 说话人分离");
            Console.WriteLine("  - Sherpa-ONNX 语音识别");
            Console.WriteLine("  - JSON 到 SRT 转换");
            Console.WriteLine();
        }

        #endregion
    }
}
