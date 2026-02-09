using DevExpress.Xpo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using TrackMenuAttributes;

namespace VT.Module.BusinessObjects
{
    public class TTSClip(Session s) : AudioClip(s)
    {
        ITTSService ttsService => Session.ServiceProvider.GetRequiredService<ITTSService>();
        IProgressService progress => Session.ServiceProvider.GetRequiredService<IProgressService>();

        /// <summary>
        /// 检查文件是否被占用
        /// </summary>
        private bool IsFileLocked(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                stream.Close();
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 生成TTS音频
        /// </summary>
        /// <param name="regenerate">是否重新生成（如果为true，即使文件存在也会重新生成）</param>
        public async Task GenerateTTS(bool regenerate = false)
        {
            // 检查是否有对应的源SRT片段
            var vadSrtClip = VadSrtClip;
            if (vadSrtClip == null)
            {
                progress.Error($"错误: 未找到对应的源字幕片段，索引 {Index}");
                return;
            }

            // 检查音频文件是否已存在（如果不重新生成）
            if (!regenerate && !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                progress.Warning($"TTS音频已存在: {FilePath}");
                return;
            }

            // 确定输出路径
            var outputDir = Path.GetDirectoryName(FilePath);
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = Path.Combine(Track.VideoProject.ProjectPath, "tts_audio");
                Directory.CreateDirectory(outputDir);
            }

            var outputPath = !string.IsNullOrEmpty(FilePath) && regenerate
                ? FilePath // 如果重新生成，使用原路径
                : Path.Combine(outputDir, $"segment_{Index:0000}_speaker_0.wav");

            // 检查目标文件是否被占用
            if (File.Exists(outputPath) && IsFileLocked(outputPath))
            {
                progress.Error($"文件正在被使用，请先停止播放后再生成: {outputPath}");
                return;
            }

            // 确定参考音频路径（来自VAD分段）
            var segmentsSourceDir = Path.Combine(Track.VideoProject.ProjectPath, "audio_segments");
            var referenceAudioPath = Path.Combine(segmentsSourceDir, $"segment_{Index:0000}.wav");

            if (!File.Exists(referenceAudioPath))
            {
                progress.Error($"错误: 找不到参考音频 {referenceAudioPath}");
                return;
            }

            // 创建TTS命令
            var command = new TTSCommand
            {
                Index = Index,
                Text = Text ?? vadSrtClip.Text,
                ReferenceAudio = referenceAudioPath,
                OutputAudio = outputPath
            };

            progress.Report($"开始生成TTS音频: 片段 {Index}");
            progress.Report($"  文本: {command.Text}");

            try
            {
                // 调用TTS服务生成音频
                var segment = await ttsService.GenerateSingleTTSAsync(command);

                if (segment != null && File.Exists(segment.AudioPath))
                {
                    // 设置音频文件
                    await SetAudioFile(segment.AudioPath);
                    progress.Report($"TTS音频生成成功: {segment.AudioPath}", MessageType.Success);
                }
                else
                {
                    progress.Error($"TTS音频生成失败: 片段 {Index}");
                }
            }
            catch (Exception ex)
            {
                progress.Error($"生成TTS音频时发生异常: {ex.Message}");
            }
        }

        #region 上下文菜单方法

        [ContextMenuAction("重新生成", Order = 10, Group = "生成")]
        public async Task RegenerateTTS()
        {
            await GenerateTTS(regenerate: true);
        }

        [ContextMenuAction("调整音频", Order = 20, Group = "调整")]
        public async Task AdjustTTS()
        {
            await Adjust(true);
        }

        #endregion
    }
}


