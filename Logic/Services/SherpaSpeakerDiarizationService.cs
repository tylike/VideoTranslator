using System.Runtime.InteropServices;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VideoTranslator.Services;

public class SherpaSpeakerDiarizationService : ServiceBase
{
    #region 配置属性

    public string SegmentationModelPath { get; set; } = string.Empty;
    public string EmbeddingModelPath { get; set; } = string.Empty;
    public int? NumClusters { get; set; }
    public float ClusteringThreshold { get; set; } = 0.5f;
    
    #endregion

    #region 私有字段

    private SherpaOnnx.OfflineSpeakerDiarization? _diarization;

    #endregion

    #region 构造函数

    public SherpaSpeakerDiarizationService() : base()
    {        
        SegmentationModelPath = Settings.SherpaSegmentationModelPath;
        EmbeddingModelPath = Settings.SherpaEmbeddingModelPath;
    }

    #endregion

    #region 公共方法

    public async Task<SpeakerDiarizationResult> DiarizeAsync(
        string audioPath,
        string language,
        int? numClusters = null,
        float? clusteringThreshold = null)
    {
        throw new Exception("当前没有实现按语言加载不同的模型的功能!");
        progress?.Report($"[SherpaSpeakerDiarizationService] 开始说话人识别");
        progress?.Report($"音频文件: {audioPath}");

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件不存在: {audioPath}");
        }

        if (!File.Exists(SegmentationModelPath))
        {
            throw new FileNotFoundException($"Segmentation模型不存在: {SegmentationModelPath}");
        }

        var effectiveEmbeddingModelPath = EmbeddingModelPath;

        if (!File.Exists(effectiveEmbeddingModelPath))
        {
            throw new FileNotFoundException($"Embedding模型不存在: {effectiveEmbeddingModelPath}");
        }

        var effectiveNumClusters = numClusters ?? NumClusters;
        var effectiveThreshold = clusteringThreshold ?? ClusteringThreshold;

        #region 音频预处理

        progress?.Report("正在预处理音频...");
        var wavPath = await ConvertAudioToWavAsync(audioPath);
        progress?.Report($"音频预处理完成: {wavPath}");

        #endregion

        #region 初始化说话人识别器

        progress?.Report("正在初始化说话人识别器...");
        InitializeDiarization(effectiveEmbeddingModelPath, effectiveNumClusters, effectiveThreshold);

        #endregion

        #region 读取音频文件

        progress?.Report("正在读取音频文件...");
        var waveReader = new WaveReader(wavPath);

        if (_diarization.SampleRate != waveReader.SampleRate)
        {
            throw new InvalidOperationException(
                $"采样率不匹配: 期望 {_diarization.SampleRate}Hz, 实际 {waveReader.SampleRate}Hz");
        }

        #endregion

        #region 执行说话人识别

        progress?.Report("正在执行说话人识别...");
        var result = await ExecuteDiarizationAsync(waveReader.Samples);

        #endregion

        #region 清理临时文件

        if (wavPath != audioPath && File.Exists(wavPath))
        {
            try
            {
                File.Delete(wavPath);
                progress?.Report($"已删除临时音频文件: {wavPath}");
            }
            catch (Exception ex)
            {
                progress?.Warning($"删除临时文件失败: {ex.Message}");
            }
        }

        #endregion

        progress?.Report($"[SherpaSpeakerDiarizationService] 识别完成，共识别到 {result.SpeakerCount} 个说话人，{result.Segments.Count} 个片段");

        return result;
    }

    #endregion

    #region 初始化

    private void InitializeDiarization(string embeddingModelPath, int? numClusters, float clusteringThreshold)
    {
        var config = new SherpaOnnx.OfflineSpeakerDiarizationConfig();
        config.Segmentation.Pyannote.Model = SegmentationModelPath;
        config.Embedding.Model = embeddingModelPath;

        if (numClusters.HasValue && numClusters.Value > 0)
        {
            config.Clustering.NumClusters = numClusters.Value;
            progress?.Report($"使用指定说话人数量: {numClusters.Value}");
        }
        else
        {
            config.Clustering.Threshold = clusteringThreshold;
            progress?.Report($"使用聚类阈值: {clusteringThreshold}");
        }

        _diarization = new SherpaOnnx.OfflineSpeakerDiarization(config);
    }

    #endregion

    #region 音频预处理

    private async Task<string> ConvertAudioToWavAsync(string audioPath)
    {
        progress?.Report($"[音频预处理] 开始转换音频文件");
        progress?.Report($"  输入文件: {audioPath}");

        var extension = Path.GetExtension(audioPath).ToLower();
        if (extension == ".wav")
        {
            progress?.Report("[音频预处理] 检测到WAV文件，检查格式...");
            var audioInfo = await CheckWavFormatAsync(audioPath);
            if (audioInfo.IsCompatible)
            {
                progress?.Report("[音频预处理] WAV格式已符合要求，无需转换");
                return audioPath;
            }
            progress?.Report($"[音频预处理] WAV格式不符合要求: 采样率={audioInfo.SampleRate}Hz, 声道={audioInfo.Channels}");
        }

        var tempDir = Path.GetTempPath();
        var tempFileName = $"sherpa_speaker_{Guid.NewGuid():N}.wav";
        var outputPath = Path.Combine(tempDir, tempFileName);

        progress?.Report($"[音频预处理] 输出文件: {outputPath}");

        var args = $"-i \"{audioPath}\" " +
                   $"-vn " +
                   $"-ar 16000 " +
                   $"-ac 1 " +
                   $"-f wav " +
                   $"-y \"{outputPath}\"";

        progress?.Report($"[音频预处理] FFmpeg参数: {args}");

        try
        {
            var ffmpegService = Ffmpeg;
            await ffmpegService.ExecuteCommandAsync(args);
            progress?.Report("[音频预处理] 音频转换成功");
            return outputPath;
        }
        catch (Exception ex)
        {
            progress?.Error($"[音频预处理] 音频转换失败: {ex.Message}");
            throw;
        }
    }

    private async Task<(bool IsCompatible, int SampleRate, int Channels)> CheckWavFormatAsync(string wavPath)
    {
        var args = $"-i \"{wavPath}\" -f null -";
        try
        {
            var ffmpegService = Ffmpeg;
            var output = await ffmpegService.ExecuteCommandAsync(args);

            int sampleRate = 0;
            int channels = 0;

            var sampleRateMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s*Hz");
            if (sampleRateMatch.Success)
            {
                int.TryParse(sampleRateMatch.Groups[1].Value, out sampleRate);
            }

            var channelsMatch = System.Text.RegularExpressions.Regex.Match(output, @"(\d+)\s*channel");
            if (channelsMatch.Success)
            {
                int.TryParse(channelsMatch.Groups[1].Value, out channels);
            }

            var isCompatible = sampleRate == 16000 && channels == 1;
            return (isCompatible, sampleRate, channels);
        }
        catch
        {
            return (false, 0, 0);
        }
    }



    #endregion

    #region 执行说话人识别

    private async Task<SpeakerDiarizationResult> ExecuteDiarizationAsync(float[] samples)
    {
        return await Task.Run(() =>
        {
            var totalChunks = samples.Length;
            var processedChunks = 0;
            var lastProgress = 0;

            var progressCallback = (int numProcessedChunks, int numTotalChunks, IntPtr arg) =>
            {
                processedChunks = numProcessedChunks;
                var p = 100.0F * numProcessedChunks / numTotalChunks;
                var currentProgress = (int)p;

                if (currentProgress > lastProgress)
                {
                    progress.Report($"处理进度: {currentProgress}%");
                    progress.ReportProgress(currentProgress);
                    lastProgress = currentProgress;
                }

                return 0;
            };

            var callback = new SherpaOnnx.OfflineSpeakerDiarizationProgressCallback(progressCallback);
            var segments = _diarization.ProcessWithCallback(samples, callback, IntPtr.Zero);

            var result = new SpeakerDiarizationResult();

            foreach (var segment in segments)
            {
                var speakerSegment = new SpeakerSegment
                {
                    Start = segment.Start,
                    End = segment.End,
                    Speaker = segment.Speaker.ToString()
                };
                result.Segments.Add(speakerSegment);
            }

            result.Segments = result.Segments.OrderBy(s => s.Start).ToList();

            return result;
        });
    }

    #endregion

    #region 清理

    public void Dispose()
    {
        _diarization?.Dispose();
        _diarization = null;
    }

    #endregion
}

#region WaveReader

public class WaveReader
{
    #region 私有字段

    private WaveHeader _header;
    private float[] _samples;

    #endregion

    #region 构造函数

    public WaveReader(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"音频文件不存在: {fileName}");
        }

        using var stream = File.Open(fileName, FileMode.Open);
        using var reader = new BinaryReader(stream);

        _header = ReadHeader(reader);

        if (!_header.Validate())
        {
            throw new InvalidOperationException($"无效的WAV文件: {fileName}");
        }

        SkipMetaData(reader);

        var buffer = reader.ReadBytes(_header.SubChunk2Size);
        var samplesInt16 = new short[_header.SubChunk2Size / 2];
        Buffer.BlockCopy(buffer, 0, samplesInt16, 0, buffer.Length);

        _samples = new float[samplesInt16.Length];

        for (var i = 0; i < samplesInt16.Length; ++i)
        {
            _samples[i] = samplesInt16[i] / 32768.0F;
        }
    }

    #endregion

    #region 公共属性

    public int SampleRate => _header.SampleRate;

    public float[] Samples => _samples;

    #endregion

    #region 私有方法

    private static WaveHeader ReadHeader(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(Marshal.SizeOf(typeof(WaveHeader)));

        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        WaveHeader header = (WaveHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(WaveHeader))!;
        handle.Free();

        return header;
    }

    private void SkipMetaData(BinaryReader reader)
    {
        var bs = reader.BaseStream;

        var subChunk2ID = _header.SubChunk2ID;
        var subChunk2Size = _header.SubChunk2Size;

        while (bs.Position != bs.Length && subChunk2ID != 0x61746164)
        {
            bs.Seek(subChunk2Size, SeekOrigin.Current);
            subChunk2ID = reader.ReadInt32();
            subChunk2Size = reader.ReadInt32();
        }

        _header.SubChunk2ID = subChunk2ID;
        _header.SubChunk2Size = subChunk2Size;
    }

    #endregion
}

#region WaveHeader

[StructLayout(LayoutKind.Sequential)]
public struct WaveHeader
{
    public int ChunkID;
    public int ChunkSize;
    public int Format;
    public int SubChunk1ID;
    public int SubChunk1Size;
    public short AudioFormat;
    public short NumChannels;
    public int SampleRate;
    public int ByteRate;
    public short BlockAlign;
    public short BitsPerSample;
    public int SubChunk2ID;
    public int SubChunk2Size;

    public bool Validate()
    {
        if (ChunkID != 0x46464952)
        {
            return false;
        }

        if (Format != 0x45564157)
        {
            return false;
        }

        if (SubChunk1ID != 0x20746d66)
        {
            return false;
        }

        if (SubChunk1Size != 16)
        {
            return false;
        }

        if (AudioFormat != 1)
        {
            return false;
        }

        if (NumChannels != 1)
        {
            return false;
        }

        if (ByteRate != (SampleRate * NumChannels * BitsPerSample / 8))
        {
            return false;
        }

        if (BlockAlign != (NumChannels * BitsPerSample / 8))
        {
            return false;
        }

        if (BitsPerSample != 16)
        {
            return false;
        }

        return true;
    }
}

#endregion
#endregion
