using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VT.Module.BusinessObjects;
using Serilog;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VideoEditor.Services;

#region BilibiliPublishHelper
/// <summary>
/// Helper service for generating Bilibili publish information using AI
/// 使用AI生成B站发布信息的辅助服务
/// </summary>
public class BilibiliPublishHelper
{
    #region Fields
    private readonly ChatService _chatService;
    private readonly IProgressService? _progressService;
    private static readonly ILogger _logger = Log.ForContext<BilibiliPublishHelper>();
    #endregion

    #region Constructor
    public BilibiliPublishHelper(IProgressService progress = null)
    {

        _chatService = new ChatService();
        _progressService = progress;
    }
    #endregion

    #region Public Methods

    /// <summary>
    /// Generates Chinese title from original title
    /// 从原标题生成中文标题
    /// </summary>
    /// <param name="title">Original title</param>
    /// <returns>Chinese title</returns>
    public async Task<string> GenerateChineseTitleAsync(string title)
    {
        try
        {
            _logger.Information("开始生成中文标题: {Title}", title);
            _progressService?.Report("正在生成中文标题...");

            var prompt = $@"请将以下视频标题翻译成中文，要求：
1. 保持标题的吸引力和专业性
2. 适合B站观众
3. 简洁明了，不超过30个字
4. 直接输出中文标题，不要加任何前缀或后缀

原标题：{title}";

            var fullResponse = await GetFullResponseAsync(prompt);
            _logger.Information("中文标题生成完成");
            return fullResponse;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "生成中文标题时发生错误");
            _progressService?.Error($"生成中文标题失败: {ex.Message}");
            return title;
        }
    }

    /// <summary>
    /// Generates description from subtitle content
    /// 从字幕内容生成简介
    /// </summary>
    /// <param name="subtitles">List of subtitles</param>
    /// <param name="title">Video title</param>
    /// <param name="youtubeUrl">YouTube source URL if applicable</param>
    /// <returns>Generated description</returns>
    public async Task<string> GenerateDescriptionAsync(List<ISrtSubtitle> subtitles, string title, string youtubeUrl = "")
    {
        try
        {
            _logger.Information("开始生成视频简介: {Title}", title);
            _progressService?.Report("正在生成视频简介...");

            var subtitleText = string.Join("\n", subtitles.Take(50).Select(s => s.Text));
            var prompt = $@"请根据以下视频字幕内容，为视频生成一个吸引人的B站简介（200-300字）。

视频标题：{title}

字幕内容：
{subtitleText}

要求：
1. 简洁明了，突出视频主题
2. 使用B站常用的表达方式
3. 适合B站观众阅读
4. 不要包含时间戳等无关信息
5. 直接输出简介内容，不要加任何前缀或后缀";

            var fullResponse = await GetFullResponseAsync(prompt);
            
            #region 添加软件介绍
            var softwareIntro = @"本视频使用一键自动配音翻译制作。
视频生成软件介绍：【开源 一键可视化 视频翻译 原声克隆 原声情感 自动校对 IndexTTS】 https://www.bilibili.com/video/BV1rL6jBUEh4/?share_source=copy_web&vd_source=11323d03e28fe3d5d656ff7d4c5662fb

有编程能力的兄弟可以尝试下，配置的内容挺多的。
https://github.com/tylike/VideoTranslator
https://github.com/tylike/index-tts

";
            fullResponse = softwareIntro + fullResponse;
            #endregion

            #region 添加YouTube源URL
            if (!string.IsNullOrEmpty(youtubeUrl))
            {
                fullResponse = $"{fullResponse}\n\n来源：{youtubeUrl}";
            }
            #endregion
            
            _logger.Information("简介生成完成");
            return fullResponse;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "生成简介时发生错误");
            _progressService?.Error($"生成简介失败: {ex.Message}");
            var defaultDescription = $"使用VideoTranslator制作的视频: {title}";
            
            #region 添加软件介绍
            var softwareIntro = @"本视频使用一键自动配音翻译制作。
视频生成软件介绍：【开源 一键可视化 视频翻译 原声克隆 原声情感 自动校对 IndexTTS】 https://www.bilibili.com/video/BV1rL6jBUEh4/?share_source=copy_web&vd_source=11323d03e28fe3d5d656ff7d4c5662fb

有编程能力的兄弟可以尝试下，配置的内容挺多的。
https://github.com/tylike/VideoTranslator
https://github.com/tylike/index-tts

";
            defaultDescription = softwareIntro + defaultDescription;
            #endregion

            if (!string.IsNullOrEmpty(youtubeUrl))
            {
                defaultDescription = $"{defaultDescription}\n\n来源：{youtubeUrl}";
            }
            return defaultDescription;
        }
    }

    /// <summary>
    /// Generates tags from subtitle content
    /// 从字幕内容生成标签
    /// </summary>
    /// <param name="subtitles">List of subtitles</param>
    /// <param name="title">Video title</param>
    /// <param name="maxTags">Maximum number of tags to generate</param>
    /// <returns>List of generated tags</returns>
    public async Task<List<string>> GenerateTagsAsync(List<ISrtSubtitle> subtitles, string title, int maxTags = 10)
    {
        try
        {
            _logger.Information("开始生成视频标签: {Title}", title);
            _progressService?.Report("正在生成视频标签...");

            var subtitleText = string.Join("\n", subtitles.Take(30).Select(s => s.Text));
            var prompt = $@"请根据以下视频字幕内容，生成{maxTags}个适合B站的标签。

视频标题：{title}

字幕内容：
{subtitleText}

要求：
1. 标签要简洁，每个标签2-5个字
2. 标签要反映视频的主题和内容
3. 使用B站常用的标签格式
4. 每行一个标签，不要加序号或其他符号
5. 直接输出标签列表，不要加任何前缀或后缀";

            var fullResponse = await GetFullResponseAsync(prompt);
            var tags = fullResponse.Split('\n')
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().TrimStart('#'))
                .Take(maxTags)
                .ToList();

            _logger.Information("标签生成完成: {Count}个标签", tags.Count);
            return tags;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "生成标签时发生错误");
            _progressService?.Error($"生成标签失败: {ex.Message}");
            return new List<string> { "视频翻译", "VT" };
        }
    }

    /// <summary>
    /// Generates publish information from video project
    /// 从视频项目生成发布信息
    /// </summary>
    /// <param name="project">Video project</param>
    /// <param name="forceRegenerate">Force regenerate even if saved info exists</param>
    /// <returns>BilibiliPublishInfo with generated content</returns>
    public async Task<VT.Module.Services.BilibiliPublishInfo> GeneratePublishInfoAsync(VideoProject project, bool forceRegenerate = false)
    {
        try
        {
            _logger.Information("开始生成发布信息: {ProjectName}", project.ProjectName);
            _progressService?.Report("正在生成发布信息...");

            var publishInfo = new VT.Module.Services.BilibiliPublishInfo
            {
                VideoFilePath = project.OutputVideoPath ?? "",
                Title = project.ProjectName ?? "未命名视频",
                Type = "自制",
                Tags = new List<string>(),
                Description = "",
                IsRepost = false,
                SourceAddress = "",
                EnableOriginalWatermark = false,
                EnableNoRepost = false
            };

            #region Use saved publish info if available
            if (!forceRegenerate && !string.IsNullOrEmpty(project.BilibiliPublishTitleChinese))
            {
                _logger.Information("使用已保存的发布信息");
                publishInfo.Title = project.BilibiliPublishTitleChinese;
                publishInfo.Description = project.BilibiliPublishDescription ?? "";
                
                if (!string.IsNullOrEmpty(project.BilibiliPublishTags))
                {
                    publishInfo.Tags = project.BilibiliPublishTags.Split(',')
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Select(t => t.Trim().TrimStart('#'))
                        .ToList();
                }

                publishInfo.Type = project.BilibiliPublishType ?? "自制";
                publishInfo.IsRepost = project.BilibiliPublishIsRepost;
                publishInfo.SourceAddress = project.BilibiliPublishSourceAddress ?? "";
                publishInfo.EnableOriginalWatermark = project.BilibiliPublishEnableOriginalWatermark;
                publishInfo.EnableNoRepost = project.BilibiliPublishEnableNoRepost;

                return publishInfo;
            }
            #endregion

            #region Generate new publish info
            #region 获取YouTube URL
            string youtubeUrl = "";
            if (project.YouTubeVideos != null && project.YouTubeVideos.Any())
            {
                var youtubeVideo = project.YouTubeVideos.FirstOrDefault();
                if (youtubeVideo != null && !string.IsNullOrEmpty(youtubeVideo.DownloadUrl))
                {
                    youtubeUrl = youtubeVideo.DownloadUrl;
                    _logger.Information("检测到YouTube源URL: {Url}", youtubeUrl);
                }
            }
            #endregion
            
            #region Use YouTube metadata if available
            if (project.YouTubeVideos != null && project.YouTubeVideos.Any())
            {
                _logger.Information("使用YouTube元数据");
                var youtubeVideo = project.YouTubeVideos.FirstOrDefault();
                if (youtubeVideo != null)
                {
                    publishInfo.Description = youtubeVideo.Description ?? "";
                    
                    #region 添加软件介绍
                    var softwareIntro = @"本视频使用一键自动配音翻译制作。
视频生成软件介绍：【开源 一键可视化 视频翻译 原声克隆 原声情感 自动校对 IndexTTS】 https://www.bilibili.com/video/BV1rL6jBUEh4/?share_source=copy_web&vd_source=11323d03e28fe3d5d656ff7d4c5662fb

有编程能力的兄弟可以尝试下，配置的内容挺多的。
https://github.com/tylike/VideoTranslator
https://github.com/tylike/index-tts

";
                    publishInfo.Description = softwareIntro + publishInfo.Description;
                    #endregion
                    
                    if (!string.IsNullOrEmpty(youtubeVideo.Tags))
                    {
                        publishInfo.Tags = youtubeVideo.Tags.Split(',')
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Select(t => t.Trim().TrimStart('#'))
                            .ToList();
                    }

                    if (!string.IsNullOrEmpty(youtubeVideo.Title))
                    {
                        publishInfo.Title = youtubeVideo.Title;
                    }

                    publishInfo.SourceAddress = youtubeVideo.DownloadUrl ?? "";
                    publishInfo.IsRepost = true;
                }
            }
            #endregion
            #region Generate from subtitles
            if (project.Tracks != null && project.Tracks.Any())
            {
                _logger.Information("从字幕生成发布信息");
                var targetSubtitleTrack = project.Tracks.FirstOrDefault(t => t.TrackType == MediaType.目标字幕);
                
                if (targetSubtitleTrack != null && targetSubtitleTrack.Segments != null && targetSubtitleTrack.Segments.Any())
                {
                    var subtitles = targetSubtitleTrack.Segments
                        .OrderBy(c => c.Index)
                        .Where(s => !string.IsNullOrEmpty(s.Text))
                        .Cast<ISrtSubtitle>()
                        .ToList();

                    if (subtitles.Any())
                    {
                        var originalTitle = project.ProjectName ?? "未命名视频";
                        var descriptionTask = GenerateDescriptionAsync(subtitles, originalTitle, youtubeUrl);
                        var tagsTask = GenerateTagsAsync(subtitles, originalTitle);
                        var chineseTitleTask = GenerateChineseTitleAsync(originalTitle);

                        await Task.WhenAll(descriptionTask, tagsTask, chineseTitleTask);

                        publishInfo.Description = await descriptionTask;
                        publishInfo.Tags = await tagsTask;
                        publishInfo.Title = await chineseTitleTask;
                    }
                }
            }
            #endregion
            #endregion

            _logger.Information("发布信息生成完成");
            return publishInfo;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "生成发布信息时发生错误");
            _progressService?.Error($"生成发布信息失败: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets full response from ChatService
    /// 获取ChatService的完整响应
    /// </summary>
    private async Task<string> GetFullResponseAsync(string prompt)
    {
        var responseChunks = new List<string>();

        await foreach (var chunk in _chatService.SendResponsesStreamAsync(prompt))
        {
            if (chunk.Success && !string.IsNullOrEmpty(chunk.ContentDelta))
            {
                responseChunks.Add(chunk.ContentDelta);
            }
        }

        return string.Join("", responseChunks).Trim();
    }

    #endregion
}
#endregion
