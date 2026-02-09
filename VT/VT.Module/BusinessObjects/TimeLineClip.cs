using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using VideoTranslator.Services;

namespace VT.Module.BusinessObjects;

[Appearance("问题", AppearanceItemType = "ViewItem", TargetItems = "问题", Criteria = "问题 != ''", BackColor = "255, 200, 200", FontColor = "0,0,0")]
[Appearance("计划调整太高", AppearanceItemType = "ViewItem", TargetItems = nameof(SpeedMultiplier), Criteria = $"{nameof(SpeedMultiplier)}>1.5", BackColor = "255, 200, 200", FontColor = "0,0,0")]
[Appearance("段首", AppearanceItemType = "ViewItem", TargetItems = nameof(Index), Criteria = $"{nameof(IsStart)}", BackColor = "255, 200, 200", FontColor = "0,0,0")]
[Appearance("英文是中文的1倍以上", AppearanceItemType = "ViewItem", TargetItems = $"{nameof(SourceSRTClip)}.{nameof(SourceSRTClip.Duration)}", Criteria = $"{nameof(SourceMoreThenTarget)}", BackColor = "255, 200, 200", FontColor = "0,0,0")]
[Appearance("未生成TTS", AppearanceItemType = "ViewItem", TargetItems = $"{nameof(TargetAudioClip)}.{nameof(TargetAudioClip.Duration)}", Criteria = $"{nameof(TargetAudioClip)}.{nameof(TargetAudioClip.Duration)}==0", BackColor = "255, 200, 200")]
public class TimeLineClip(Session s) : VTBaseObject(s)
{
    protected bool SourceMoreThenTarget
    {
        get
        {
            if ((SourceSRTClip?.Duration ?? 0) <= 0)
                return false;
            if ((TargetAudioClip?.Duration ?? 0) <= 0)
                return false;
            return (SourceSRTClip.Duration / TargetAudioClip.Duration) >= 2;
        }
    }
    [XafDisplayName("索引")]
    public int Index
    {
        get { return GetPropertyValue<int>(nameof(Index)); }
        set { SetPropertyValue(nameof(Index), value); }
    }

    [XafDisplayName("视频项目")]
    [Association]
    public VideoProject VideoProject
    {
        get { return GetPropertyValue<VideoProject>(nameof(VideoProject)); }
        set { SetPropertyValue(nameof(VideoProject), value); }
    }

    public override void AfterConstruction()
    {
        base.AfterConstruction();
        this.SourceSRTClip = new SRTClip(Session);
        this.SourceAudioClip = new AudioClip(Session);
        this.TargetAudioClip = new AudioClip(Session);
        this.TargetSRTClip = new SRTClip(Session);
        this.AdjustedTargetAudioClip = new AudioClip(Session);
    }

    #region child clip
    [XafDisplayName("源字幕")]
    [Persistent, Aggregated]
    public SRTClip SourceSRTClip
    {
        get { return GetPropertyValue<SRTClip>(nameof(SourceSRTClip)); }
        protected set { SetPropertyValue(nameof(SourceSRTClip), value); }
    }

    [XafDisplayName("源音频")]
    [Persistent, Aggregated]
    public AudioClip SourceAudioClip
    {
        get { return GetPropertyValue<AudioClip>(nameof(SourceAudioClip)); }
        protected set { SetPropertyValue(nameof(SourceAudioClip), value); }
    }

    [XafDisplayName("目标字幕")]
    [Persistent, Aggregated]
    public SRTClip TargetSRTClip
    {
        get { return GetPropertyValue<SRTClip>(nameof(TargetSRTClip)); }
        protected set { SetPropertyValue(nameof(TargetSRTClip), value); }
    }

    [XafDisplayName("目标音频")]
    [Persistent, Aggregated]
    public AudioClip TargetAudioClip
    {
        get { return GetPropertyValue<AudioClip>(nameof(TargetAudioClip)); }
        protected set { SetPropertyValue(nameof(TargetAudioClip), value); }
    }

    [XafDisplayName("计划调速")]
    [ModelDefault("DisplayFormat", "#.000")]
    public double SpeedMultiplier
    {
        get { return GetPropertyValue<double>(nameof(SpeedMultiplier)); }
        set { SetPropertyValue(nameof(SpeedMultiplier), value); }
    }

    [XafDisplayName("调整后音频")]
    [Persistent, Aggregated]
    public AudioClip AdjustedTargetAudioClip
    {
        get { return GetPropertyValue<AudioClip>(nameof(AdjustedTargetAudioClip)); }
        protected set { SetPropertyValue(nameof(AdjustedTargetAudioClip), value); }
    }
    #endregion

    [Size(-1)]
    [ModelDefault("RowCount", "1")]
    [XafDisplayName("G翻译")]
    public string TranslateEngineResult
    {
        get { return GetPropertyValue<string>(nameof(TranslateEngineResult)); }
        set { SetPropertyValue(nameof(TranslateEngineResult), value); }
    }


    [XafDisplayName("已调整")]
    public bool AudioAdjusted
    {
        get { return GetPropertyValue<bool>(nameof(AudioAdjusted)); }
        set { SetPropertyValue(nameof(AudioAdjusted), value); }
    }

    [XafDisplayName("速度可接受")]
    public bool SpeedAcceptable
    {
        get { return SpeedMultiplier <= 1.5 && SpeedMultiplier >= 0.5; }
    }

    [XafDisplayName("生成状态")]
    public string GenerationStatus
    {
        get { return GetPropertyValue<string>(nameof(GenerationStatus)); }
        set { SetPropertyValue(nameof(GenerationStatus), value); }
    }

    [XafDisplayName("进度消息")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "2")]
    public string ProgressMessage
    {
        get { return GetPropertyValue<string>(nameof(ProgressMessage)); }
        set { SetPropertyValue(nameof(ProgressMessage), value); }
    }

    [XafDisplayName("最后输出")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "3")]
    public string LastOutput
    {
        get { return GetPropertyValue<string>(nameof(LastOutput)); }
        set { SetPropertyValue(nameof(LastOutput), value); }
    }

    public bool IsStart
    {
        get
        {
            //如果这是第一段，或，本clip.start与上一个clip.end不相等
            if (this.Index == 0) return true;
            var before = this.VideoProject.Clips.FirstOrDefault(x => x.Index == this.Index - 1);
            if (before == null)
                return true;
            if (before.SourceSRTClip.End == this.SourceSRTClip.Start) return false;
            return true;
        }
    }

    public void SetGenerationStatus(string status, string message = null)
    {
        GenerationStatus = status;
        if (message != null)
        {
            ProgressMessage = message;
        }
    }

    public void SetProcessing(string message)
    {
        SetGenerationStatus("正在生成...", message);
        LastOutput = "";
    }

    public void SetCompleted(string message = null)
    {
        SetGenerationStatus("已生成", message);
    }

    public void SetError(string errorMessage)
    {
        SetGenerationStatus("生成失败", $"错误: {errorMessage}");
    }

    public void AppendOutput(string output)
    {
        LastOutput += output + "\n";
    }

    public bool HasSourceAudio()
    {
        return SourceAudioClip != null && !string.IsNullOrEmpty(SourceAudioClip.FilePath) && File.Exists(SourceAudioClip.FilePath);
    }

    public bool HasTargetAudio()
    {
        return TargetAudioClip != null && !string.IsNullOrEmpty(TargetAudioClip.FilePath) && File.Exists(TargetAudioClip.FilePath);
    }

    public bool HasAdjustedTargetAudio()
    {
        return AdjustedTargetAudioClip != null && !string.IsNullOrEmpty(AdjustedTargetAudioClip.FilePath) && File.Exists(AdjustedTargetAudioClip.FilePath);
    }

    public bool HasTargetSubtitle()
    {
        return TargetSRTClip != null && !string.IsNullOrEmpty(TargetSRTClip.Text);
    }
    IAudioService audioService => Session.ServiceProvider.GetRequiredService<IAudioService>();
    public async Task Adjust(string adjustedDir, AudioInfo audioInfo)
    {
        var subtitle = SourceSRTClip; //subtitles.FirstOrDefault(s => s.Index == timeLineClip.SourceSRTClip.Index);
        if (subtitle != null)
        {
            var targetAudioPath = TargetAudioClip.FilePath;
            if (!string.IsNullOrEmpty(targetAudioPath) && File.Exists(targetAudioPath))
            {
                var segmentInfo = await audioService.GetAudioInfoAsync(targetAudioPath);
                var timeDiff = segmentInfo.DurationSeconds - subtitle.Duration;

                Debug.WriteLine($"  片段 {subtitle.Index}:");
                Debug.WriteLine($"    英文时长: {subtitle.Duration:F2}s");
                Debug.WriteLine($"    中文时长: {segmentInfo.DurationSeconds:F2}s");
                Debug.WriteLine($"    差异: {timeDiff:+F2}s");

                var adjustedFile = Path.Combine(adjustedDir, $"adjusted_{subtitle.Index:0000}.wav");

                if (Math.Abs(timeDiff) < 0)
                {
                    Debug.WriteLine($"    时长差异很小，直接使用原音频");
                    File.Copy(targetAudioPath, adjustedFile, true);
                    SpeedMultiplier = 1.0;
                    AudioAdjusted = false;
                }
                else if (timeDiff > 0)
                {
                    Debug.WriteLine($"    中文比英文长，需要快放");

                    if (subtitle.Duration <= 0)
                    {
                        Debug.WriteLine($"    错误: 字幕时长无效 ({subtitle.Duration:F4}s)，跳过快放调整");
                        File.Copy(targetAudioPath, adjustedFile, true);
                        SpeedMultiplier = 1.0;
                        AudioAdjusted = false;
                        goto AfterAudioAdjust;
                    }

                    if (subtitle.Duration < 0.1)
                    {
                        Debug.WriteLine($"    警告: 字幕时长太短 ({subtitle.Duration:F4}s={subtitle.Duration * 1000:F2}ms)，可能数据异常");
                    }

                    var targetDurationMs = subtitle.Duration * 1000;
                    Debug.WriteLine($"    目标时长: {targetDurationMs:F2}ms");

                    await audioService.AdjustAudioToDurationAsync(
                        targetAudioPath,
                        targetDurationMs,
                        segmentInfo.SampleRate,
                        segmentInfo.Channels,
                        adjustedFile);
                    SpeedMultiplier = segmentInfo.DurationSeconds / subtitle.Duration;
                    AudioAdjusted = true;
                }
                else
                {
                    Debug.WriteLine($"    中文比英文短或相等，无需调整，直接使用原音频");
                    File.Copy(targetAudioPath, adjustedFile, true);
                    SpeedMultiplier = 1.0;
                    AudioAdjusted = false;
                }

            AfterAudioAdjust:
                var adjustedAudioClip = AdjustedTargetAudioClip;
                adjustedAudioClip.Index = subtitle.Index;
                adjustedAudioClip.Start = subtitle.Start;
                adjustedAudioClip.End = subtitle.End;

                await adjustedAudioClip.SetAudioFile(adjustedFile);


                SetCompleted("已调整");

            }
        }
    }

    public string 问题
    {
        get
        {
            var adjustedDuration = (int)Math.Round((AdjustedTargetAudioClip?.Duration ?? 0) * 1000);
            var sourceDuration = (int)Math.Round((SourceSRTClip?.Duration ?? 0) * 1000);
            if (adjustedDuration > sourceDuration + 20)
                return "调整后音频太长";
            return "";
        }
    }

    #region 字幕翻译相关方法

    /// <summary>
    /// 应用翻译结果到当前片段
    /// </summary>
    public void ApplyTranslation()
    {
        if (!string.IsNullOrEmpty(TranslateEngineResult))
        {
            TargetSRTClip.Text = TranslateEngineResult;
        }
    }

    #endregion

    #region TTS相关方法

    /// <summary>
    /// 更新TTS音频文件路径
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    public async Task UpdateTTSAudio(string audioPath)
    {
        await TargetAudioClip.SetAudioFile(audioPath);
        SetCompleted();
    }

    #endregion

}
