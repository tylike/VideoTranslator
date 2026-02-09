using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using Serilog;
using TrackMenuAttributes;
using VideoTranslator.Interfaces;

namespace VT.Module.BusinessObjects;

public partial class AudioClip : IWaveform
{
    [ContextMenuAction("显示/隐藏波形", IsAutoCommit = true)]
    public void ShowHideWaveform()
    {
        this.ShowWaveform = !ShowWaveform;
    }

    #region 波形字段

    private int _waveformSamplesPerSecond = 80;
    private static readonly ILogger _waveformLogger = Log.ForContext<AudioClip>();

    #endregion

    #region 波形属性

    List<double> IWaveform.WaveformData
    {
        get
        {
            if (field == null)
            {
                try
                {
                    field = WaveformService.LoadWaveformData(this.FilePath, _waveformSamplesPerSecond);
                }
                catch (Exception ex)
                {
                    _waveformLogger.Error(ex,"出错了!");
                }

            }
            return field;
        }
        set
        {
            field = value;
        }
    }

    [XafDisplayName("显示波形")]
    public bool ShowWaveform
    {
        get => field;
        set => SetPropertyValue("ShowWaveform", ref field, value);
    }

    int IWaveform.WaveformSamplesPerSecond => _waveformSamplesPerSecond;

    #endregion   
}

public static class WaveformService
{
    public static List<double> LoadWaveformData(string filePath,int perSecondSampleCount)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new Exception($"文件路径为空，跳过波形加载: ClipIndex={filePath}");
        }
        using var audioFile = new AudioFileReader(filePath);
        int sampleCount = (int)(perSecondSampleCount * audioFile.TotalTime.TotalSeconds);
        if(sampleCount < 0)
        {
            sampleCount = 1;
        }
        var samples = new List<float>();
        var buffer = new float[audioFile.WaveFormat.SampleRate * audioFile.WaveFormat.Channels];
        int bytesRead;

        while ((bytesRead = audioFile.Read(buffer, 0, buffer.Length)) > 0)
        {
            samples.AddRange(buffer.Take(bytesRead));
        }

        double duration = audioFile.TotalTime.TotalSeconds;
        int actualSampleCount = sampleCount;
        var waveform = NormalizeWaveform(samples, actualSampleCount);

        if (waveform.Count > 0)
        {
            var min = waveform.Min();
            var max = waveform.Max();
            var avg = waveform.Average();
        }
        else
        {
            throw new Exception($"[WaveformService] 波形数据为空: FilePath={filePath}");
        }
        return waveform;

    }

    private static List<double> NormalizeWaveform(List<float> samples, int targetCount)
    {
        if (samples.Count == 0)
        {
            return new List<double>();
        }

        var result = new List<double>(targetCount);
        int samplesPerPoint = Math.Max(1, samples.Count / targetCount);

        for (int i = 0; i < targetCount; i++)
        {
            int startIndex = i * samplesPerPoint;
            int endIndex = Math.Min(startIndex + samplesPerPoint, samples.Count);

            if (startIndex >= samples.Count)
            {
                break;
            }

            var chunk = samples.Skip(startIndex).Take(endIndex - startIndex).ToList();
            double amplitude = chunk.Any() ? chunk.Max(Math.Abs) : 0;
            result.Add(amplitude);
        }

        double maxAmplitude = result.Max();
        if (maxAmplitude > 0)
        {
            for (int i = 0; i < result.Count; i++)
            {
                result[i] = result[i] / maxAmplitude;
            }
        }

        return result;
    }
}