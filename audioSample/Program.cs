﻿﻿﻿using AudioSample.Services;
using AudioSample.Utils;

namespace AudioSample
{
    class Program
    {
        #region 主入口

        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  音频识别整合系统");
            Console.WriteLine("  整合 Whisper + Sherpa-ONNX 多引擎优势");
            Console.WriteLine("========================================");
            Console.WriteLine();

            #region 参数处理

            string audioPath;
            if (args.Length > 0)
            {
                audioPath = args[0];
            }
            else
            {
                audioPath = @"d:\VideoTranslator\videoProjects\5\source_audio.flac";
                Console.WriteLine($"使用默认音频路径: {audioPath}");
                Console.WriteLine();
            }

            if (!File.Exists(audioPath))
            {
                Console.WriteLine($"错误: 音频文件不存在: {audioPath}");
                return;
            }

            #endregion

            #region 配置选项

            var useVAD = true;
            var useSpeakerDiarization = false;
            var useWordTimestamps = true;

            Console.WriteLine("识别选项:");
            Console.WriteLine($"  1. 使用 VAD (静音检测): {(useVAD ? "是" : "否")}");
            Console.WriteLine($"  2. 使用说话人识别: {(useSpeakerDiarization ? "是" : "否")}");
            Console.WriteLine($"  3. 使用词级时间戳: {(useWordTimestamps ? "是" : "否")}");
            Console.WriteLine();

            #endregion

            #region 创建服务并执行

            var service = new TranscriptionService();

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var result = await service.TranscribeAudioAsync(
                    audioPath,
                    useVAD: useVAD,
                    useSpeakerDiarization: useSpeakerDiarization,
                    useWordTimestamps: useWordTimestamps
                );

                stopwatch.Stop();

                #region 显示结果摘要

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("  识别完成");
                Console.WriteLine("========================================");
                Console.WriteLine($"总时长: {result.TotalDuration:F2} 秒");
                Console.WriteLine($"说话人数: {result.SpeakerCount}");
                Console.WriteLine($"片段数: {result.Segments.Count}");
                Console.WriteLine($"处理时间: {stopwatch.Elapsed.TotalSeconds:F2} 秒");
                Console.WriteLine();

                #endregion

                #region 显示部分结果

                Console.WriteLine("前5个片段预览:");
                Console.WriteLine("----------------------------------------");
                for (int i = 0; i < Math.Min(5, result.Segments.Count); i++)
                {
                    var segment = result.Segments[i];
                    var speakerPrefix = segment.SpeakerId.HasValue ? $"[Speaker {segment.SpeakerId.Value}] " : "";
                    Console.WriteLine($"{i + 1}. [{AudioUtils.FormatTime(segment.StartTime)} - {AudioUtils.FormatTime(segment.EndTime)}] {speakerPrefix}{segment.Text}");
                }

                if (result.Segments.Count > 5)
                {
                    Console.WriteLine($"... 还有 {result.Segments.Count - 5} 个片段");
                }

                Console.WriteLine("----------------------------------------");
                Console.WriteLine();

                #endregion

                #region 显示词级时间戳示例

                if (result.Segments.Any(s => s.WordTimestamps.Any()))
                {
                    Console.WriteLine("词级时间戳示例 (第一个片段):");
                    Console.WriteLine("----------------------------------------");
                    var firstSegment = result.Segments.First(s => s.WordTimestamps.Any());
                    foreach (var word in firstSegment.WordTimestamps.Take(10))
                    {
                        Console.WriteLine($"  [{AudioUtils.FormatTime(word.StartTime)} - {AudioUtils.FormatTime(word.EndTime)}] {word.Word}");
                    }
                    if (firstSegment.WordTimestamps.Count > 10)
                    {
                        Console.WriteLine($"  ... 还有 {firstSegment.WordTimestamps.Count - 10} 个词");
                    }
                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine();
                }

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }

            #endregion

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        #endregion
    }
}
