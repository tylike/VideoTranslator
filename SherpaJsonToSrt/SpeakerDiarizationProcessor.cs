using System.Text.RegularExpressions;

namespace SherpaJsonToSrt
{
    #region 说话人分离配置

    public class SpeakerDiarizationConfig
    {
        public string SherpaDiarizationPath { get; set; } = @"D:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\bin\sherpa-onnx-offline-speaker-diarization.exe";
        public string SegmentationModel { get; set; } = @"D:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\sherpa-onnx-pyannote-segmentation-3-0\model.onnx";
        public string EmbeddingModel { get; set; } = @"D:\VideoTranslator\第三方项目\stt\sherpa-onnx.models\3dspeaker_speech_eres2net_sv_en_voxceleb_16k.onnx";
        public double ClusterThreshold { get; set; } = 0.5;
    }

    #endregion

    #region 说话人信息

    public class SpeakerSegment
    {
        public int SpeakerId { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }

        public override string ToString()
        {
            return $"{SpeakerId} {StartTime:F2} {EndTime:F2}";
        }
    }

    #endregion

    #region 说话人分离处理器

    public class SpeakerDiarizationProcessor
    {
        private readonly SpeakerDiarizationConfig _config;
        private readonly ProcessExecutor _executor;

        public SpeakerDiarizationProcessor(SpeakerDiarizationConfig? config = null)
        {
            _config = config ?? new SpeakerDiarizationConfig();
            _executor = new ProcessExecutor();
        }

        #region 执行说话人分离

        public CommandResult Process(string audioPath, string outputPath)
        {
            #region 构建命令参数

            var arguments = $"--segmentation.pyannote-model={_config.SegmentationModel} " +
                          $"--embedding.model={_config.EmbeddingModel} " +
                          $"--clustering.cluster-threshold={_config.ClusterThreshold} " +
                          $"\"{audioPath}\"";

            #endregion

            #region 执行分离

            Console.WriteLine($"执行说话人分离...");
            Console.WriteLine($"  音频文件: {audioPath}");
            Console.WriteLine($"  输出文件: {outputPath}");
            Console.WriteLine($"  分割模型: {_config.SegmentationModel}");
            Console.WriteLine($"  嵌入模型: {_config.EmbeddingModel}");
            Console.WriteLine();

            var result = _executor.Execute(_config.SherpaDiarizationPath, arguments);

            #endregion

            #region 过滤并保存结果

            if (result.Success)
            {
                var filteredOutput = FilterSpeakerSegments(result.StandardOutput);
                File.WriteAllText(outputPath, filteredOutput, System.Text.Encoding.UTF8);
                Console.WriteLine($"说话人分离完成，输出已保存到: {outputPath}");
            }

            #endregion

            return result;
        }

        #endregion

        #region 异步执行说话人分离

        public async Task<CommandResult> ProcessAsync(string audioPath, string outputPath)
        {
            return await Task.Run(() => Process(audioPath, outputPath));
        }

        #endregion

        #region 过滤说话人片段

        private string FilterSpeakerSegments(string output)
        {
            #region 使用正则表达式过滤以数字开头的行

            var lines = output.Split('\n', '\r');
            var filteredLines = new List<string>();
            var pattern = @"^\d";

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine) && Regex.IsMatch(trimmedLine, pattern))
                {
                    filteredLines.Add(trimmedLine);
                }
            }

            #endregion

            return string.Join(Environment.NewLine, filteredLines);
        }

        #endregion

        #region 解析说话人片段

        public List<SpeakerSegment> ParseSegments(string filePath)
        {
            var segments = new List<SpeakerSegment>();

            if (!File.Exists(filePath))
            {
                return segments;
            }

            #region 读取并解析文件

            var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
            var pattern = @"^(\d+\.?\d*)\s*--\s*(\d+\.?\d*)\s+speaker_(\d+)";

            foreach (var line in lines)
            {
                var match = Regex.Match(line.Trim(), pattern);
                if (match.Success)
                {
                    segments.Add(new SpeakerSegment
                    {
                        SpeakerId = int.Parse(match.Groups[3].Value),
                        StartTime = double.Parse(match.Groups[1].Value),
                        EndTime = double.Parse(match.Groups[2].Value)
                    });
                }
            }

            #endregion

            return segments;
        }

        #endregion
    }

    #endregion
}
