using System.Linq;

namespace SherpaJsonToSrt
{
    #region 批处理配置

    public class BatchProcessConfig
    {
        public WhisperConfig? WhisperConfig { get; set; }
        public SpeakerDiarizationConfig? SpeakerDiarizationConfig { get; set; }
        public SherpaAsrConfig? SherpaAsrConfig { get; set; }
        public bool RunWhisper { get; set; } = true;
        public bool RunSpeakerDiarization { get; set; } = true;
        public bool RunSherpaAsr { get; set; } = true;

        #region 说话人分离多阈值配置

        public List<double> SpeakerClusterThresholds { get; set; } = new List<double> { 0.5, 0.8,0.9 };

        #endregion
    }

    #endregion

    #region 批处理结果

    public class BatchProcessResult
    {
        public bool WhisperSuccess { get; set; }
        public bool SpeakerDiarizationSuccess { get; set; }
        public bool SherpaAsrSuccess { get; set; }
        public bool SrtConversionSuccess { get; set; }
        public string? WhisperOutputPath { get; set; }
        public string? SpeakerOutputPath { get; set; }
        public string? SherpaJsonOutputPath { get; set; }
        public string? SherpaSrtOutputPath { get; set; }

        #region 多阈值说话人分离结果

        public List<SpeakerDiarizationResult> SpeakerDiarizationResults { get; set; } = new List<SpeakerDiarizationResult>();

        #endregion

        public bool AllSuccess => WhisperSuccess && SpeakerDiarizationSuccess && SherpaAsrSuccess && SrtConversionSuccess;
    }

    #endregion

    #region 说话人分离结果

    public class SpeakerDiarizationResult
    {
        public double ClusterThreshold { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int SpeakerCount { get; set; }
    }

    #endregion

    #region 音频批处理器

    public class AudioBatchProcessor
    {
        private readonly WhisperProcessor _whisperProcessor;
        private readonly SpeakerDiarizationProcessor _speakerProcessor;
        private readonly SherpaAsrProcessor _sherpaProcessor;
        private readonly BatchProcessConfig _config;

        public AudioBatchProcessor(BatchProcessConfig? config = null)
        {
            _config = config ?? new BatchProcessConfig();

            _whisperProcessor = new WhisperProcessor(_config.WhisperConfig);
            _speakerProcessor = new SpeakerDiarizationProcessor(_config.SpeakerDiarizationConfig);
            _sherpaProcessor = new SherpaAsrProcessor(_config.SherpaAsrConfig);
        }

        #region 处理音频文件

        public BatchProcessResult Process(string audioPath)
        {
            #region 验证输入

            if (!File.Exists(audioPath))
            {
                Console.WriteLine($"错误: 音频文件不存在: {audioPath}");
                return new BatchProcessResult();
            }

            #endregion

            #region 准备输出路径

            var audioDir = Path.GetDirectoryName(audioPath) ?? string.Empty;
            var audioName = Path.GetFileNameWithoutExtension(audioPath);

            var result = new BatchProcessResult
            {
                WhisperOutputPath = Path.Combine(audioDir, $"{audioName}_whisper_word.srt"),
                SpeakerOutputPath = Path.Combine(audioDir, $"{audioName}_speaker.txt"),
                SherpaJsonOutputPath = Path.Combine(audioDir, $"{audioName}_sherpa.json"),
                SherpaSrtOutputPath = Path.Combine(audioDir, $"{audioName}_sherpa.srt")
            };

            #endregion

            #region 显示处理信息

            PrintHeader(audioPath, audioDir);

            #endregion

            #region 步骤1: Whisper 识别

            if (_whisperProcessor != null)
            {
                result.WhisperSuccess = ProcessWhisper(audioPath, audioDir, audioName);
            }

            #endregion

            #region 步骤2: 说话人分离

            if (_speakerProcessor != null)
            {
                result.SpeakerDiarizationSuccess = ProcessSpeakerDiarization(audioPath, audioDir, audioName, result);
            }

            #endregion

            #region 步骤3: Sherpa-ONNX 识别

            if (_sherpaProcessor != null)
            {
                result.SherpaAsrSuccess = ProcessSherpaAsr(audioPath, result.SherpaJsonOutputPath, result.SherpaSrtOutputPath);
                result.SrtConversionSuccess = result.SherpaAsrSuccess;
            }

            #endregion

            #region 显示处理结果

            PrintSummary(result, audioName);

            #endregion

            return result;
        }

        #endregion

        #region 异步处理音频文件

        public async Task<BatchProcessResult> ProcessAsync(string audioPath)
        {
            return await Task.Run(() => Process(audioPath));
        }

        #endregion

        #region 处理 Whisper 识别

        private bool ProcessWhisper(string audioPath, string audioDir, string audioName)
        {
            PrintStepHeader(1, 3, "Whisper Word-Level SRT");

            var outputPrefix = Path.Combine(audioDir, $"{audioName}_whisper");
            var result = _whisperProcessor.Process(audioPath, outputPrefix);

            if (result.Success)
            {
                Console.WriteLine("Whisper 识别完成!");
                Console.WriteLine($"输出: {audioName}_whisper_word.srt");
            }
            else
            {
                Console.WriteLine("Whisper 识别失败!");
            }

            Console.WriteLine();
            return result.Success;
        }

        #endregion

        #region 处理说话人分离

        private bool ProcessSpeakerDiarization(string audioPath, string audioDir, string audioName, BatchProcessResult result)
        {
            PrintStepHeader(2, 3, "Speaker Diarization");

            #region 处理多个阈值

            var allSuccess = true;

            foreach (var threshold in _config.SpeakerClusterThresholds)
            {
                #region 配置当前阈值

                var baseConfig = _config.SpeakerDiarizationConfig ?? new SpeakerDiarizationConfig();
                var speakerConfig = new SpeakerDiarizationConfig
                {
                    SherpaDiarizationPath = baseConfig.SherpaDiarizationPath,
                    SegmentationModel = baseConfig.SegmentationModel,
                    EmbeddingModel = baseConfig.EmbeddingModel,
                    ClusterThreshold = threshold
                };

                var processor = new SpeakerDiarizationProcessor(speakerConfig);
                var outputPath = Path.Combine(audioDir, $"{audioName}_speaker_t{threshold:F1}.txt");

                #endregion

                #region 执行分离

                Console.WriteLine($"使用阈值 {threshold:F1} 进行说话人分离...");
                var processResult = processor.Process(audioPath, outputPath);

                #endregion

                #region 解析说话人数量

                var speakerCount = 0;
                if (processResult.Success && File.Exists(outputPath))
                {
                    var segments = processor.ParseSegments(outputPath);
                    speakerCount = segments.Select(s => s.SpeakerId).Distinct().Count();
                }

                #endregion

                #region 保存结果

                result.SpeakerDiarizationResults.Add(new SpeakerDiarizationResult
                {
                    ClusterThreshold = threshold,
                    OutputPath = outputPath,
                    Success = processResult.Success,
                    SpeakerCount = speakerCount
                });

                #endregion

                #region 显示结果

                if (processResult.Success)
                {
                    Console.WriteLine($"阈值 {threshold:F1}: 识别完成，输出: {Path.GetFileName(outputPath)}");
                    Console.WriteLine($"  识别到 {speakerCount} 个说话人");
                }
                else
                {
                    Console.WriteLine($"阈值 {threshold:F1}: 分离失败!");
                    allSuccess = false;
                }

                Console.WriteLine();

                #endregion
            }

            #endregion

            #region 保持默认输出路径兼容性

            if (result.SpeakerDiarizationResults.Count > 0)
            {
                result.SpeakerOutputPath = result.SpeakerDiarizationResults[0].OutputPath;
            }

            #endregion

            return allSuccess;
        }

        #endregion

        #region 处理 Sherpa-ONNX 识别

        private bool ProcessSherpaAsr(string audioPath, string jsonOutputPath, string srtOutputPath)
        {
            PrintStepHeader(3, 3, "Sherpa-ONNX Recognition");

            var result = _sherpaProcessor.Process(audioPath, jsonOutputPath);

            if (result.Success)
            {
                Console.WriteLine("Sherpa-ONNX 识别完成!");
                Console.WriteLine($"JSON输出: {Path.GetFileName(jsonOutputPath)}");
                Console.WriteLine();

                var conversionSuccess = _sherpaProcessor.ConvertJsonToSrt(jsonOutputPath, srtOutputPath);
                if (conversionSuccess)
                {
                    Console.WriteLine($"SRT输出: {Path.GetFileName(srtOutputPath)}");
                }
                else
                {
                    Console.WriteLine("SRT转换失败!");
                }
            }
            else
            {
                Console.WriteLine("Sherpa-ONNX 识别失败!");
            }

            Console.WriteLine();
            return result.Success;
        }

        #endregion

        #region 打印处理信息

        private void PrintHeader(string audioPath, string audioDir)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Audio Recognition Batch Tool");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine($"输入: {audioPath}");
            Console.WriteLine($"输出: {audioDir}");
            Console.WriteLine();
        }

        private void PrintStepHeader(int step, int total, string title)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"  Step {step}/{total}: {title}");
            Console.WriteLine("========================================");
            Console.WriteLine();
        }

        private void PrintSummary(BatchProcessResult result, string audioName)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  Processing Complete");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("生成的文件:");
            Console.WriteLine($"  1. {audioName}_whisper_word.srt (Whisper word-level)");

            #region 显示说话人分离结果

            if (result.SpeakerDiarizationResults.Count > 0)
            {
                Console.WriteLine($"  2. 说话人分离结果:");
                foreach (var speakerResult in result.SpeakerDiarizationResults)
                {
                    var fileName = Path.GetFileName(speakerResult.OutputPath);
                    Console.WriteLine($"     - {fileName} (阈值: {speakerResult.ClusterThreshold:F1}, 说话人: {speakerResult.SpeakerCount})");
                }
            }
            else
            {
                Console.WriteLine($"  2. {audioName}_speaker.txt (Speaker diarization)");
            }

            #endregion

            Console.WriteLine($"  3. {audioName}_sherpa.json (Sherpa JSON)");
            Console.WriteLine($"  4. {audioName}_sherpa.srt (Sherpa SRT)");
            Console.WriteLine();
        }

        #endregion
    }

    #endregion
}
