using AudioSample.Integrators;
using AudioSample.Models;
using AudioSample.Parsers;
using AudioSample.Utils;

namespace AudioSample.Services
{
    public class TranscriptionService
    {
        #region 配置属性

        public string WhisperPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\whisper.cpp.exe-1.8.3\whisper-cli.exe";
        public string WhisperModelPath { get; set; } = @"d:\VideoTranslator\whisper.cpp\ggml-large-v3.bin";
        public string SherpaBinPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\bin";
        public string SherpaVADModelPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-pyannote-segmentation-3-0\model.onnx";
        public string SherpaWhisperEncoderPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-whisper-large-v3\large-v3-encoder.int8.onnx";
        public string SherpaWhisperDecoderPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-whisper-large-v3\large-v3-decoder.int8.onnx";
        public string SherpaWhisperTokensPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-whisper-large-v3\large-v3-tokens.txt";

        #endregion

        #region 公共方法

        public async Task<IntegratedTranscriptionResult> TranscribeAudioAsync(
            string audioPath,
            bool useVAD = true,
            bool useSpeakerDiarization = false,
            bool useWordTimestamps = true,
            string? outputDir = null)
        {
            Console.WriteLine($"开始处理音频文件: {audioPath}");

            #region 准备输出目录

            if (outputDir == null)
            {
                outputDir = Path.GetDirectoryName(audioPath);
            }

            var baseFileName = Path.GetFileNameWithoutExtension(audioPath);
            var tempDir = Path.Combine(outputDir!, "temp");
            Directory.CreateDirectory(tempDir);

            #endregion

            #region 步骤1: 使用 Whisper 进行语音识别（获取准确内容）

            Console.WriteLine("步骤1: 使用 Whisper 进行语音识别...");
            var whisperResult = await RunWhisperAsync(audioPath, tempDir, baseFileName);
            Console.WriteLine($"Whisper 识别完成，共 {whisperResult.Segments.Count} 个片段");

            #endregion

            #region 步骤2: 使用 Sherpa-ONNX VAD 进行静音检测（获取精确时间边界）

            SherpaVADResult? vadResult = null;
            if (useVAD)
            {
                Console.WriteLine("步骤2: 使用 Sherpa-ONNX VAD 进行静音检测...");
                vadResult = await RunSherpaVADAsync(audioPath);
                Console.WriteLine($"VAD 检测完成，共 {vadResult.Segments.Count} 个语音片段");
            }

            #endregion

            #region 步骤3: 使用 Sherpa-ONNX 进行说话人识别（获取说话人信息）

            SherpaSpeakerResult? speakerResult = null;
            if (useSpeakerDiarization)
            {
                Console.WriteLine("步骤3: 使用 Sherpa-ONNX 进行说话人识别...");
                speakerResult = await RunSherpaSpeakerDiarizationAsync(audioPath);
                Console.WriteLine($"说话人识别完成，共 {speakerResult.Segments.Count} 个说话人片段");
            }

            #endregion

            #region 步骤4: 使用 Sherpa-ONNX 进行词级时间戳识别（获取细粒度时间对齐）

            SherpaTranscriptionResult? wordTimestampResult = null;
            if (useWordTimestamps)
            {
                Console.WriteLine("步骤4: 使用 Sherpa-ONNX 进行词级时间戳识别...");
                wordTimestampResult = await RunSherpaTranscriptionAsync(audioPath);
                Console.WriteLine($"词级时间戳识别完成，共 {wordTimestampResult.WordTimestamps.Count} 个词");
            }

            #endregion

            #region 步骤5: 整合所有结果

            Console.WriteLine("步骤5: 整合所有识别结果...");
            var integratedResult = TranscriptionIntegrator.Integrate(
                whisperResult,
                vadResult,
                speakerResult,
                wordTimestampResult
            );
            Console.WriteLine($"整合完成，共 {integratedResult.Segments.Count} 个片段，{integratedResult.SpeakerCount} 个说话人");

            #endregion

            #region 步骤6: 导出结果

            Console.WriteLine("步骤6: 导出结果...");
            await ExportResultsAsync(integratedResult, outputDir!, baseFileName);

            #endregion

            #region 清理临时文件

            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理临时文件失败: {ex.Message}");
            }

            #endregion

            return integratedResult;
        }

        #endregion

        #region 私有方法 - Whisper 执行

        private async Task<WhisperResult> RunWhisperAsync(string audioPath, string tempDir, string baseFileName)
        {
            var outputPath = Path.Combine(tempDir, $"{baseFileName}_whisper.txt");
            var srtPath = Path.Combine(tempDir, $"{baseFileName}_whisper.srt");

            var arguments = $"-m \"{WhisperModelPath}\" -f \"{audioPath}\" -otxt -osrt -of \"{Path.Combine(tempDir, baseFileName)}_whisper\" -l auto";

            var result = await AudioUtils.RunProcessAsync(WhisperPath, arguments, 600000);

            Console.WriteLine($"Whisper 输出: {result.Output}");
            Console.WriteLine($"Whisper 错误: {result.Error}");

            var whisperResult = await WhisperParser.ParseFromOutputAsync(result.Output);

            if (File.Exists(srtPath))
            {
                var srtResult = await WhisperParser.ParseFromSrtAsync(srtPath);
                if (srtResult.Segments.Count > whisperResult.Segments.Count)
                {
                    whisperResult = srtResult;
                }
            }

            return whisperResult;
        }

        #endregion

        #region 私有方法 - Sherpa-ONNX VAD 执行

        private async Task<SherpaVADResult> RunSherpaVADAsync(string audioPath)
        {
            var vadExePath = Path.Combine(SherpaBinPath, "sherpa-onnx-vad.exe");
            var vadModelPath = Path.Combine(SherpaBinPath, "silero_vad.onnx");

            if (!File.Exists(vadModelPath))
            {
                Console.WriteLine("Sherpa VAD 模型不存在，跳过 VAD 检测");
                return new SherpaVADResult();
            }

            var arguments = $"--silero-vad-model=\"{vadModelPath}\" \"{audioPath}\" nul";

            var result = await AudioUtils.RunProcessAsync(vadExePath, arguments, 300000);

            return await SherpaParser.ParseVADOutputAsync(result.Output);
        }

        #endregion

        #region 私有方法 - Sherpa-ONNX 说话人识别执行

        private async Task<SherpaSpeakerResult> RunSherpaSpeakerDiarizationAsync(string audioPath)
        {
            var diarizationExePath = Path.Combine(SherpaBinPath, "sherpa-onnx-offline-speaker-diarization.exe");

            var arguments = $"--segmentation.pyannote-model=\"{SherpaVADModelPath}\" --clustering.cluster-threshold=0.90 \"{audioPath}\"";

            var result = await AudioUtils.RunProcessAsync(diarizationExePath, arguments, 300000);

            return await SherpaParser.ParseSpeakerDiarizationAsync(result.Output);
        }

        #endregion

        #region 私有方法 - Sherpa-ONNX 词级时间戳执行

        private async Task<SherpaTranscriptionResult> RunSherpaTranscriptionAsync(string audioPath)
        {
            var offlineExePath = Path.Combine(SherpaBinPath, "sherpa-onnx-offline.exe");

            var arguments = $"--tokens=\"{SherpaWhisperTokensPath}\" --whisper-encoder=\"{SherpaWhisperEncoderPath}\" --whisper-decoder=\"{SherpaWhisperDecoderPath}\" --model-type=whisper \"{audioPath}\"";

            var result = await AudioUtils.RunProcessAsync(offlineExePath, arguments, 600000);

            return await SherpaParser.ParseTranscriptionOutputAsync(result.Output);
        }

        #endregion

        #region 私有方法 - 导出结果

        private async Task ExportResultsAsync(IntegratedTranscriptionResult result, string outputDir, string baseFileName)
        {
            var srtPath = Path.Combine(outputDir, $"{baseFileName}_integrated.srt");
            var jsonPath = Path.Combine(outputDir, $"{baseFileName}_integrated.json");
            var txtPath = Path.Combine(outputDir, $"{baseFileName}_integrated.txt");
            var detailedPath = Path.Combine(outputDir, $"{baseFileName}_integrated_detailed.txt");

            await File.WriteAllTextAsync(srtPath, TranscriptionIntegrator.ExportToSRT(result));
            await File.WriteAllTextAsync(jsonPath, TranscriptionIntegrator.ExportToJson(result));
            await File.WriteAllTextAsync(txtPath, TranscriptionIntegrator.ExportToText(result));
            await File.WriteAllTextAsync(detailedPath, TranscriptionIntegrator.ExportToDetailedText(result));

            Console.WriteLine($"结果已导出到:");
            Console.WriteLine($"  - SRT: {srtPath}");
            Console.WriteLine($"  - JSON: {jsonPath}");
            Console.WriteLine($"  - TXT: {txtPath}");
            Console.WriteLine($"  - 详细TXT: {detailedPath}");
        }

        #endregion
    }
}
