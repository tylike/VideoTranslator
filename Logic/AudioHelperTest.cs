using VideoTranslator.Utils;

namespace VideoTranslator.Logic;

class AudioHelperTest
{
    public static void Run(string[] args)
    {
        #region 测试音频文件路径

        var testFiles = new List<string>
        {
            @"d:\VideoTranslator\videoProjects\65\merged_audio.wav",
            @"d:\VideoTranslator\videoProjects\66\BV1xHFWzxEuE_audio.mp3"
        };

        #endregion

        #region 显示标题

        Console.WriteLine("========================================");
        Console.WriteLine("  AudioHelper 测试程序");
        Console.WriteLine("  使用 NAudio 获取音频时长");
        Console.WriteLine("========================================");
        Console.WriteLine();

        #endregion

        #region 测试每个音频文件

        foreach (var audioPath in testFiles)
        {
            if (!File.Exists(audioPath))
            {
                Console.WriteLine($"跳过不存在的文件: {audioPath}");
                Console.WriteLine();
                continue;
            }

            Console.WriteLine($"测试文件: {Path.GetFileName(audioPath)}");
            Console.WriteLine($"完整路径: {audioPath}");
            Console.WriteLine();

            #region 测试格式检查

            Console.WriteLine("  [1] 格式检查:");
            var isSupported = AudioHelper.IsSupportedAudioFormat(audioPath);
            Console.WriteLine($"      是否支持: {isSupported}");
            Console.WriteLine();

            #endregion

            #region 测试获取时长（秒）

            Console.WriteLine("  [2] 获取时长（秒）:");
            try
            {
                var durationSeconds = AudioHelper.GetAudioDurationSeconds(audioPath);
                Console.WriteLine($"      时长: {durationSeconds:F4} 秒");
                Console.WriteLine($"      时长: {durationSeconds * 1000:F2} 毫秒");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      错误: {ex.Message}");
                Console.WriteLine();
                continue;
            }

            #endregion

            #region 测试获取时长（TimeSpan）

            Console.WriteLine("  [3] 获取时长（TimeSpan）:");
            try
            {
                var duration = AudioHelper.GetAudioDuration(audioPath);
                Console.WriteLine($"      时长: {duration}");
                Console.WriteLine($"      格式化: {AudioHelper.FormatDuration(duration)}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      错误: {ex.Message}");
                Console.WriteLine();
            }

            #endregion

            #region 测试获取时长（毫秒）

            Console.WriteLine("  [4] 获取时长（毫秒）:");
            try
            {
                var durationMs = AudioHelper.GetAudioDurationMilliseconds(audioPath);
                Console.WriteLine($"      时长: {durationMs} 毫秒");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      错误: {ex.Message}");
                Console.WriteLine();
            }

            #endregion

            #region 测试获取完整音频信息

            Console.WriteLine("  [5] 获取完整音频信息:");
            try
            {
                var audioInfo = AudioHelper.GetAudioInfo(audioPath);
                Console.WriteLine($"      {audioInfo}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      错误: {ex.Message}");
                Console.WriteLine();
            }

            #endregion

            Console.WriteLine("----------------------------------------");
            Console.WriteLine();
        }

        #endregion

        #region 测试格式化功能

        Console.WriteLine("========================================");
        Console.WriteLine("  格式化功能测试");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var testDurations = new[]
        {
            0.123,
            1.5,
            30.456,
            125.789,
            3665.123
        };

        foreach (var duration in testDurations)
        {
            var formatted = AudioHelper.FormatDuration(duration);
            Console.WriteLine($"  {duration:F3} 秒 -> {formatted}");
        }

        Console.WriteLine();
        Console.WriteLine("----------------------------------------");
        Console.WriteLine();

        #endregion

        #region 测试异常处理

        Console.WriteLine("========================================");
        Console.WriteLine("  异常处理测试");
        Console.WriteLine("========================================");
        Console.WriteLine();

        Console.WriteLine("  [1] 测试空路径:");
        try
        {
            AudioHelper.GetAudioDurationSeconds("");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      预期异常: {ex.GetType().Name}");
            Console.WriteLine($"      消息: {ex.Message}");
        }
        Console.WriteLine();

        Console.WriteLine("  [2] 测试不存在的文件:");
        try
        {
            AudioHelper.GetAudioDurationSeconds("nonexistent.wav");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      预期异常: {ex.GetType().Name}");
            Console.WriteLine($"      消息: {ex.Message}");
        }
        Console.WriteLine();

        Console.WriteLine("  [3] 测试不支持的格式:");
        try
        {
            var isSupported = AudioHelper.IsSupportedAudioFormat("test.txt");
            Console.WriteLine($"      是否支持: {isSupported}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      异常: {ex.GetType().Name}");
            Console.WriteLine($"      消息: {ex.Message}");
        }
        Console.WriteLine();

        #endregion

        #region 测试完成

        Console.WriteLine("========================================");
        Console.WriteLine("  测试完成");
        Console.WriteLine("========================================");

        #endregion
    }
}
