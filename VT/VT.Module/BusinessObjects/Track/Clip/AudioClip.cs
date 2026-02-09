﻿using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VT.Module.Controllers;
using TrackMenuAttributes;

namespace VT.Module.BusinessObjects;

public partial class AudioClip(Session s) : Clip(s)
{
    /// <summary>
    /// 调整后的音频片段
    /// </summary>
    public AudioClip AdjustedAudioClip
    {
        get { return field; }
        set { SetPropertyValue("AdjustedAudioClip", ref field, value); }
    }

    public override MediaType Type => MediaType.Audio;    

    [XafDisplayName("文件路径")]
    [Size(SizeAttribute.Unlimited)]
    [Persistent]
    public string FilePath
    {
        get { return GetPropertyValue<string>(nameof(FilePath)); }
        protected set 
        { 
            SetPropertyValue(nameof(FilePath), value);
        }
    }

    [XafDisplayName("速度倍数")]
    public double SpeedMultiplier
    {
        get { return GetPropertyValue<double>(nameof(SpeedMultiplier)); }
        set { SetPropertyValue(nameof(SpeedMultiplier), value); }
    }

    /// <summary>
    /// 此音频与字幕的对应关系
    /// </summary>
    public SRTClip VadSrtClip
    {
        get { return field; }
        set { SetPropertyValue("VadSrtClip", ref field, value); }
    }
    
    #region 计算属性

    [XafDisplayName("超时时长比例")]
    public double OverDurationRatio
    {
        get
        {
            if (VadSrtClip == null || VadSrtClip.Duration <= 0)
            {
                return 0;
            }
            return Duration / VadSrtClip.Duration - 1.0;
        }
    }

    [XafDisplayName("背景颜色")]
    public override string BackgroundColor
    {
        get
        {
            if (OverDurationRatio <= 0)
            {
                return "Transparent";
            }

            double intensity = Math.Min(OverDurationRatio, 1.0);

            byte alpha = (byte)(intensity * 200 + 55);
            byte red = 255;
            byte green = (byte)(255 - intensity * 200);
            byte blue = (byte)(255 - intensity * 200);

            return $"#{alpha:X2}{red:X2}{green:X2}{blue:X2}";
        }
    }

    [XafDisplayName("工具提示信息")]
    public override string ToolTipInfo
    {
        get
        {
            var info = base.ToolTipInfo;

            if (!string.IsNullOrEmpty(FilePath))
            {
                info += $"\n文件: {System.IO.Path.GetFileName(FilePath)}";
            }

            if (VadSrtClip != null)
            {
                info += $"\n参考SRT: {VadSrtClip.Index}";
            }

            return info;
        }
    }

    #endregion

    IAudioService audioService => Session.ServiceProvider.GetRequiredService<IAudioService>();
    IProgressService progress => Session.ServiceProvider.GetRequiredService<IProgressService>();

    public async Task SetAudioFile(string filePath)
    {
        #region 原始音频处理
        FilePath = filePath;
        try
        {
            var duration = await audioService.GetAudioDurationSecondsAsyn(filePath);
            this.End = this.Start + TimeSpan.FromSeconds(duration);
        }
        catch (Exception ex)
        {
            progress.Error($"获取音频时长失败: {ex.Message}");
            this.End = this.Start;
        }
        #endregion

        #region 去除音频开头和结尾的静音:暂时不需要支除静音

        if (false)
        {
            try
            {
                progress.Report("开始检测并去除音频静音...");
                var inputDir = Path.GetDirectoryName(filePath);
                var inputFileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var inputExt = Path.GetExtension(filePath);

                var outputFileName = $"ns_{(inputFileNameWithoutExt.Length > 8 ? inputFileNameWithoutExt.Substring(8) : inputFileNameWithoutExt)}{inputExt}";
                var outputPath = Path.Combine(inputDir, outputFileName);

                await audioService.RemoveLeadingTrailingSilenceAsync(filePath, outputPath, 30);

                FilePath = outputPath;
                var newDuration = await audioService.GetAudioDurationSecondsAsyn(outputPath);
                this.End = this.Start + TimeSpan.FromSeconds(newDuration);
            }
            catch (Exception ex)
            {
                progress.Error($"去除静音失败: {ex.Message}");
            }
        }
        #endregion
    }
    [Action]
    public async Task ComputeDuration()
    {
        this.End = TimeSpan.FromSeconds(await audioService.GetAudioDurationSecondsAsyn(FilePath));
    }

    public bool AudioAdjusted
    {
        get { return field; }
        set { SetPropertyValue("AudioAdjusted", ref field, value); }
    }

    /// <summary>
    /// 将当前音频片段调整为与字幕片段时长一致
    /// </summary>
    /// <param name="使用尾部可用空白时长">使用末尾的可用空白时间</param>
    /// <returns>调整后的片段</returns>
    public async Task<AudioClip> Adjust(bool 使用尾部可用空白时长)
    {
        this.FilePath.ValidateFileExists("待调整的音频必须存在!");
        string adjustedDir = Path.Combine(this.Track.VideoProject.ProjectPath, "adjusted");
        Directory.CreateDirectory(adjustedDir);
        //var adjustedFilePath = Path.Combine(adjustedDir, Path.GetFileName(this.FilePath));

        var subtitle = (Track as AudioTrackInfo).RelationSrt.Segments.FirstOrDefault(s => s.Index == this.Index);
        if (subtitle != null)
        {
            var adjustedFile = Path.Combine(adjustedDir, $"adjusted_{subtitle.Index:0000}.wav");

            var 待处理音频文件 = this.FilePath;
            if (!string.IsNullOrEmpty(待处理音频文件) && File.Exists(待处理音频文件))
            {
                #region 计算时长差异
                var 待处理音频信息 = await audioService.GetAudioInfoAsync(待处理音频文件);
                double 时长差异 = 0;
                double 可用时长 = subtitle.Duration; //先取英文的时长做为默认值
                var 下一段开始时间 = 0d;
                if (使用尾部可用空白时长)
                {
                    //有下一段，下一段的开始时间大于本段的结束时间
                    if (subtitle.NextClip != null && subtitle.NextClip.Start.TotalSeconds > subtitle.End.TotalSeconds)
                    {
                        可用时长 = subtitle.NextClip.Start.TotalSeconds - subtitle.Start.TotalSeconds;
                        下一段开始时间 = subtitle.NextClip.Start.TotalSeconds;
                    }
                }
                //结果: 大于0,说明太长了,0:正好,小于0，说明中文短，无需处理
                时长差异 = 待处理音频信息.DurationSeconds - 可用时长;
                #endregion

                #region 输出调试信息
                Debug.WriteLine($"  片段 {subtitle.Index}[{subtitle.Start.TotalSeconds:F2}-{subtitle.End.TotalSeconds:F2}({下一段开始时间:F2})]:{subtitle.Text}");
                Debug.WriteLine($"    英文时长: {subtitle.Duration:F2}s");
                Debug.WriteLine($"    中文时长: {待处理音频信息.DurationSeconds:F2}s");
                Debug.WriteLine($"    目标时长: {可用时长:F2}s");
                Debug.WriteLine($"    差异: {时长差异:+F2}s");
                #endregion

                #region 音频调整逻辑
                if (Math.Abs(时长差异) < 0.01)
                {
                    Debug.WriteLine($"    时长差异很小，直接使用原音频");
                    File.Copy(待处理音频文件, adjustedFile, true);
                    SpeedMultiplier = 1.0;
                    AudioAdjusted = false;
                }
                else if (时长差异 > 0)
                {
                    Debug.WriteLine($"    中文比英文长，需要快放");

                    if (可用时长 <= 0)
                    {
                        progress.Error($"错误:目标时长无效 ({可用时长:F4}s)，跳过快放调整");
                        File.Copy(待处理音频文件, adjustedFile, true);
                        SpeedMultiplier = 1.0;
                        AudioAdjusted = false;
                        goto AfterAudioAdjust;
                    }

                    if (可用时长 < 0.1)
                    {
                        progress.Warning($"警告: 目标时长太短 ({可用时长:F4}s={可用时长 * 1000:F2}ms)，可能数据异常");
                    }

                    var targetDurationMs = 可用时长 * 1000;
                    Debug.WriteLine($"    目标时长: {targetDurationMs:F2}ms");

                    await audioService.AdjustAudioToDurationAsync(
                        待处理音频文件,
                        targetDurationMs,
                        待处理音频信息.SampleRate,
                        待处理音频信息.Channels,
                        adjustedFile);
                    SpeedMultiplier = 待处理音频信息.DurationSeconds / 可用时长;
                    AudioAdjusted = true;
                }
                else
                {
                    Debug.WriteLine($"    中文比英文短或相等，无需调整，直接使用原音频");
                    File.Copy(待处理音频文件, adjustedFile, true);
                    SpeedMultiplier = 1.0;
                    AudioAdjusted = false;
                }
                #endregion
            }
        AfterAudioAdjust:
            AudioClip adjustedAudioClip = this.AdjustedAudioClip ?? new AudioClip(Session);
            adjustedAudioClip.Index = subtitle.Index;
            adjustedAudioClip.Start = subtitle.Start;
            adjustedAudioClip.End = subtitle.End;
            await adjustedAudioClip.SetAudioFile(adjustedFile);
            this.AdjustedAudioClip = adjustedAudioClip;
            this.AdjustedAudioClip.VadSrtClip = this.VadSrtClip;
            return adjustedAudioClip;
        }
        else
        {
            throw new Exception($"未找到对应的字幕片段，无法调整音频片段 {this.Index}");
        }
    }

    #region 上下文菜单方法

    [ContextMenuAction("调整音频速度", Order = 10, Group = "调整")]
    public async Task AdjustAudioSpeed()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            progress.Warning("音频文件不存在，无法调整");
            return;
        }

        await Adjust(false);
    }

    [ContextMenuAction("调整音频速度(使用末尾空白)", Order = 20, Group = "调整")]
    public async Task AdjustAudioSpeedWithEndEmptyTime()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            progress.Warning("音频文件不存在，无法调整");
            return;
        }

        await Adjust(true);
    }

    [ContextMenuAction("重新计算时长", Order = 30, Group = "调整")]
    public async Task RecomputeDuration()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            progress.Warning("音频文件不存在，无法重新计算时长");
            return;
        }

        await ComputeDuration();
    }

    [ContextMenuAction("导出音频", Order = 10, Group = "导出")]
    public void ExportAudio()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            progress.Warning("音频文件不存在，无法导出");
            return;
        }

        var exportDir = Path.Combine(this.Track.VideoProject.ProjectPath, "export");
        Directory.CreateDirectory(exportDir);

        var exportFile = Path.Combine(exportDir, $"audio_{Index:0000}.wav");
        File.Copy(FilePath, exportFile, true);

        progress.Report($"音频已导出到: {exportFile}", MessageType.Info);
    }

    [ContextMenuAction("打开文件所在位置", Order = 20, Group = "导出")]
    public void OpenFileLocation()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            progress.Warning("音频文件不存在");
            return;
        }

        if (File.Exists(FilePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{FilePath}\"",
                UseShellExecute = true
            });
        }
    }

    [ContextMenuAction("播放音频", Order = 10, Group = "播放")]
    public void PlayAudio()
    {
    }

    #endregion
}

