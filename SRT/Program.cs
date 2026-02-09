using System;
using VideoTranslator.SRT.Services;
using VideoTranslator.SRT.Models;

namespace VideoTranslator.SRT
{
    class Program
    {
        static void Main(string[] args)
        {
            #region 参数处理

            if (args.Length < 1)
            {
                PrintUsage();
                return;
            }

            string jsonFilePath = args[0];
            string outputFilePath = args.Length > 1 ? args[1] : null;
            string mode = args.Length > 2 ? args[2].ToLower() : "segment";

            bool useWordLevelTimestamps = mode == "--word-level";
            bool useTtsOptimized = mode == "--tts-optimized";

            #endregion

            #region 执行转换

            try
            {
                Console.WriteLine("Starting SRT generation...");

                var parser = new WhisperJsonParser();
                SrtGenerator generator;

                Console.WriteLine($"Parsing JSON file: {jsonFilePath}");
                var whisperData = parser.Parse(jsonFilePath);

                Console.WriteLine("Generating SRT content...");
                string srtContent;

                if (useTtsOptimized)
                {
                    Console.WriteLine("Using TTS-optimized mode with smart filtering...");
                    var config = new TtsOptimizationConfig();
                    generator = new SrtGenerator(config);
                    srtContent = generator.GenerateSrtForTts(whisperData);
                }
                else if (useWordLevelTimestamps)
                {
                    Console.WriteLine("Using word-level timestamps...");
                    generator = new SrtGenerator();
                    srtContent = generator.GenerateSrtWithWordLevelTimestamps(whisperData);
                }
                else
                {
                    Console.WriteLine("Using segment-level timestamps...");
                    generator = new SrtGenerator();
                    srtContent = generator.GenerateSrt(whisperData);
                }

                if (string.IsNullOrEmpty(outputFilePath))
                {
                    outputFilePath = System.IO.Path.ChangeExtension(jsonFilePath, ".srt");
                }

                Console.WriteLine($"Saving SRT file to: {outputFilePath}");
                generator.SaveSrtToFile(srtContent, outputFilePath);

                Console.WriteLine("SRT generation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }

            #endregion
        }

        #region 私有方法

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: VideoTranslator.SRT.exe <json_file_path> [output_srt_path] [mode]");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  json_file_path      Path to the whisper.cpp JSON output file");
            Console.WriteLine("  output_srt_path     (Optional) Path for the output SRT file");
            Console.WriteLine("  mode                (Optional) Generation mode:");
            Console.WriteLine("                       --segment         (default) Segment-level timestamps");
            Console.WriteLine("                       --word-level      Word-level timestamps");
            Console.WriteLine("                       --tts-optimized   TTS-optimized with smart filtering");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  VideoTranslator.SRT.exe output.json");
            Console.WriteLine("  VideoTranslator.SRT.exe output.json subtitles.srt");
            Console.WriteLine("  VideoTranslator.SRT.exe output.json subtitles.srt --word-level");
            Console.WriteLine("  VideoTranslator.SRT.exe output.json subtitles.srt --tts-optimized");
        }

        #endregion
    }
}
