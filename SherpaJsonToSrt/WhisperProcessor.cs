namespace SherpaJsonToSrt
{
    #region Whisper 配置

    public class WhisperConfig
    {
        public string WhisperCliPath { get; set; } = @"D:\VideoTranslator\第三方项目\stt\whisper.cpp.exe-1.8.3\whisper-cli.exe";
        public string ModelPath { get; set; } = @"D:\VideoTranslator\whisper.cpp\ggml-large-v3-turbo-q8_0.bin";
        public int MaxLen { get; set; } = 1;
        public string Language { get; set; } = "auto";
        public bool Translate { get; set; } = true;
    }

    #endregion

    #region Whisper 处理器

    public class WhisperProcessor
    {
        private readonly WhisperConfig _config;
        private readonly ProcessExecutor _executor;

        public WhisperProcessor(WhisperConfig? config = null)
        {
            _config = config ?? new WhisperConfig();
            _executor = new ProcessExecutor();
        }

        #region 执行 Whisper 识别

        public CommandResult Process(string audioPath, string outputPrefix)
        {
            #region 构建命令参数

            var arguments = $"-m \"{_config.ModelPath}\" -f \"{audioPath}\" -osrt --max-len {_config.MaxLen} -of \"{outputPrefix}\" -l {_config.Language}";

            if (_config.Translate)
            {
                arguments += " -tr";
            }

            #endregion

            #region 执行识别

            Console.WriteLine($"执行 Whisper 识别...");
            Console.WriteLine($"  音频文件: {audioPath}");
            Console.WriteLine($"  输出前缀: {outputPrefix}");
            Console.WriteLine($"  模型: {_config.ModelPath}");
            Console.WriteLine();

            var result = _executor.Execute(_config.WhisperCliPath, arguments);

            #endregion

            return result;
        }

        #endregion

        #region 异步执行 Whisper 识别

        public async Task<CommandResult> ProcessAsync(string audioPath, string outputPrefix)
        {
            return await Task.Run(() => Process(audioPath, outputPrefix));
        }

        #endregion

        #region 获取输出文件路径

        public string GetOutputSrtPath(string outputPrefix)
        {
            return $"{outputPrefix}_word.srt";
        }

        #endregion
    }

    #endregion
}
