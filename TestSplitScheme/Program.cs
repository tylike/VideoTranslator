using VideoTranslator.Models;
using VideoEditor.Models;
using VideoEditor.Services;
using System.Text.Json;

Console.WriteLine("音频分割算法测试...");
Console.WriteLine("=".PadRight(60, '='));

var vadJsonPath = @"D:\video.download\28\vad_info.json";
TestWithRealData(vadJsonPath);

void TestWithRealData(string jsonPath)
{
    #region 准备数据

    var jsonContent = File.ReadAllText(jsonPath);
    var vadResult = JsonSerializer.Deserialize<VadDetectionResult>(jsonContent, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    if (vadResult == null)
    {
        Console.WriteLine("无法解析VAD数据");
        return;
    }

    #endregion

    #region 显示VAD检测结果

    DisplayVadResult(vadResult);

    #endregion

    #region 执行音频分段

    var splitter = new AudioSplitter();
    var segments = splitter.SplitAudio(vadResult);

    #endregion

    #region 显示分段结果

    DisplaySplitResult(segments);

    #endregion
}

void DisplayVadResult(VadDetectionResult vadResult)
{
    decimal min = 1.0M;

    #region 显示语音片段

    foreach (var item in vadResult.Segments)
    {
        if (item.IsSpeech)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            var time = TimeSpan.FromSeconds((long)item.Start);
            Console.Write($"[{time.TotalMinutes:F1}]");
            Console.WriteLine($"{item.Start}-{item.End} {item.Duration}");
        }
        else
        {
            #region 显示静音片段

            if (item.Duration > min)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"静音: {item.Duration:F2}秒");
            }

            #endregion
        }
    }

    #endregion

    #region 显示统计信息

    Console.WriteLine($"\nVAD检测结果:");
    Console.WriteLine($"音频总时长: {vadResult.AudioDuration:F2}秒 ({vadResult.AudioDuration / 60:F2}分钟)");
    Console.WriteLine($"语音片段数: {vadResult.SpeechSegmentCount}");
    Console.WriteLine($"静音片段数: {vadResult.SilenceSegmentCount}");
    Console.WriteLine($"语音总时长: {vadResult.TotalSpeechDuration:F2}秒");
    Console.WriteLine($"静音总时长: {vadResult.TotalSilenceDuration:F2}秒");

    #endregion
}

void DisplaySplitResult(List<AudioSegment> segments)
{
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("\n分段结果:");

    #region 显示每个分段

    int i = 0;
    foreach (var item in segments)
    {
        var durationMinutes = item.Duration / 60;
        var timeStart = TimeSpan.FromSeconds((long)item.Start);
        var timeEnd = TimeSpan.FromSeconds((long)item.End);

        if (item.SplitSilenceDuration > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{i++}: {timeStart:hh\\:mm\\:ss} - {timeEnd:hh\\:mm\\:ss} ({durationMinutes:F2}分钟) [分段静音: {item.SplitSilenceDuration:F2}秒]");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{i++}: {timeStart:hh\\:mm\\:ss} - {timeEnd:hh\\:mm\\:ss} ({durationMinutes:F2}分钟)");
        }
    }

    #endregion

    #region 显示分段统计

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"\n共有分段: {segments.Count}");

    #endregion
}
