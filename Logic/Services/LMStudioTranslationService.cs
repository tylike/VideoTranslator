using System.Text;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VideoTranslator.Services;

public enum TranslationStyle
{
    Professional,
    Casual,
    Humorous
}

public class LMStudioTranslationService : ServiceBase
{
    private readonly ChatService _chatService;
    public TranslationStyle CurrentStyle { get; set; } = TranslationStyle.Professional;

    private static readonly Dictionary<TranslationStyle, string> StyleSystemPrompts = new()
    {
        {
            TranslationStyle.Professional,
            "你是一个专业的视频字幕翻译助手。请将以下{源语言}字幕翻译成{目标语言}。要求：\n" +
            "1. 翻译要准确、自然、符合口语习惯\n" +
            "2. 保持专业术语的准确性（如足球术语）\n" +
            "3. 2025这样的年份翻译为\"二零二五\"\n" +
            "4. 翻译结果要简洁，适合TTS朗读\n" +
            "5. 必须逐条翻译，每条字幕一行，格式为：序号. 翻译内容\n" +
            "6. 必须翻译所有输入的字幕，不能遗漏\n" +
            "7. 必须保持序号与输入完全一致\n" +
            "8. 不要添加任何额外说明或空行\n" +
            $"9. 【重要】必须返回完整的{{0}}条翻译，不能少任何一条"+
            "内容是来自语音识别的，可能有识别错误，可以根据上下文修复明显的错误"
        },
        {
            TranslationStyle.Casual,
            "你是一个接地气的字幕翻译助手。用轻松自然的口语化{目标语言}翻译字幕。要求：\n" +
            "1. 翻译要像日常聊天一样自然，别太书面化\n" +
            "2. 遇到数字年份直接读出来，比如2025年就说\"二零二五年\"\n" +
            "3. 翻译要简洁，TTS读起来不费劲\n" +
            "4. 逐条翻译，每条一行，格式：序号. 翻译内容\n" +
            "5. 所有字幕都要翻译，一个都不能少\n" +
            "6. 序号必须和原文一一对应，不能乱\n" +
            "7. 别加任何废话和空行\n" +
            $"8. 【重要】必须给老子返回完整的{{0}}条翻译，一条都不能少！"+
            "内容是来自语音识别的，可能有识别错误，可以根据上下文修复明显的错误"
        },
        {
            TranslationStyle.Humorous,
            "你是一个超爱开玩笑的沙雕字幕翻译！把{源语言}字幕翻译成搞笑的{目标语言}。要求：\n" +
            "1. 翻译要幽默风趣，能让人笑出声最好\n" +
            "2. 可以适当玩梗，但别太出格\n" +
            "3. 遇到数字年份就按口语来，比如2025年就说\"二零二五\"\n" +
            "4. 翻译要简洁，TTS念着顺口\n" +
            "5. 逐条翻译，每条一行，格式：序号. 搞笑翻译\n" +
            "6. 每条都必须翻译，一个都别想跑\n" +
            "7. 序号必须和原文完全对得上，不许乱来\n" +
            "8. 别加任何解说和空行，保持队形\n" +
            $"9. 【重要】必须返回完整的{{0}}条翻译，少一条算你输！"+
            "内容是来自语音识别的，可能有识别错误，可以根据上下文修复明显的错误"
        }
    };

    public LMStudioTranslationService() : base()
    {
        _chatService = new ChatService();
        progress?.Report($"[LMStudioTranslationService] 初始化完成，API地址: {Config.VideoTranslator.LLM.ApiUrl}，模型: {Config.VideoTranslator.LLM.ModelName}");
    }

    public void SetTranslationStyle(TranslationStyle style)
    {
        CurrentStyle = style;
        progress?.Report($"[LMStudioTranslationService] 翻译风格已设置为: {style}");
    }

    public string GetEffectiveSystemPrompt(TranslationStyle style, string? customPrompt = null, Language sl = Language.English, Language tl = Language.Chinese)
    {
        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            progress?.Report($"[LMStudioTranslationService] 使用自定义提示词，长度: {customPrompt.Length}");
            return customPrompt;
        }
        var sld = GetLanguageDisplayName(sl);
        var tld = GetLanguageDisplayName(tl);
        if (StyleSystemPrompts.TryGetValue(style, out var prompt))
        {
            progress?.Report($"[LMStudioTranslationService] 使用预设风格提示词: {style}");

            return prompt.Replace("{源语言}", sld).Replace("{目标语言}", tld);
        }

        progress?.Report($"[LMStudioTranslationService] 未找到风格 {style} 的提示词，使用默认值");
        var defaultPrompt = StyleSystemPrompts[TranslationStyle.Professional];

        return defaultPrompt.Replace("{源语言}", sld).Replace("{目标语言}", sld);
    }

    public string? GetSystemPromptOnly(TranslationStyle style)
    {
        if (StyleSystemPrompts.TryGetValue(style, out var prompt))
        {
            return prompt;
        }
        return null;
    }

    public async Task<List<ISrtSubtitle>> TranslateSubtitlesAsync(
        IEnumerable<ISrtSubtitle> subtitles,
        TranslationApi api,
        string? systemPrompt,
        int? batchSize,
        Language targetLanguage,
        Language sourceLanguage = Language.Auto)
    {
        progress?.Report("正在准备字幕翻译...");

        progress?.Report($"[LMStudioTranslationService] 开始翻译字幕");
        progress?.Report($"共 {subtitles.Count()} 条字幕待翻译");
        progress?.Report($"源语言: {sourceLanguage}");
        progress?.Report($"目标语言: {targetLanguage}");

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            progress?.Report($"使用自定义提示词，长度: {systemPrompt.Length}");
        }
        else
        {
            progress?.Report($"使用当前风格提示词: {CurrentStyle}");
        }

        var actualBatchSize = batchSize ?? 30;
        var totalBatches = (int)Math.Ceiling((double)subtitles.Count() / actualBatchSize);
        var translatedSubtitles = new List<ISrtSubtitle>();

        for (int i = 0; i < subtitles.Count(); i += actualBatchSize)
        {
            var batchEnd = Math.Min(i + actualBatchSize, subtitles.Count());
            var batch = subtitles.Skip(i).Take(batchEnd - i).ToList();
            var batchIndex = i / actualBatchSize + 1;
            var progressPercentage = (double)batchEnd / subtitles.Count() * 100;

            progress?.Report($"\n处理批次 {batchIndex}/{totalBatches}: 第{i + 1}-{batchEnd}条");

            progress?.ReportProgress(progressPercentage, $"正在翻译字幕 [{i + 1}-{batchEnd}/{subtitles.Count()}]...");

            var result = await TranslateBatchWithRetry(batch, batchIndex, totalBatches, systemPrompt, sourceLanguage, targetLanguage);

            if (result != null)
            {
                translatedSubtitles.AddRange(result);
                progress?.Report($"✓ 批次 {batchIndex} 翻译完成");
            }
            else
            {
                progress?.Warning($"批次 {batchIndex} 完全失败，使用原文");
                foreach (var sub in batch)
                {
                    translatedSubtitles.Add(new SrtSubtitle
                    {
                        Index = sub.Index,
                        StartTime = sub.StartTime,
                        EndTime = sub.EndTime,
                        Text = $"[翻译失败: API调用失败]"
                    });
                }
            }
        }

        progress?.Report("字幕翻译完成！");

        var failedCount = translatedSubtitles.Count(s => s.Text.StartsWith("[翻译失败"));
        var successCount = translatedSubtitles.Count - failedCount;

        progress?.Report($"\n翻译完成!");
        progress?.Report($"总字幕数: {subtitles.Count()}");
        progress?.Report($"翻译成功: {successCount}");
        progress?.Report($"翻译失败: {failedCount}");

        return translatedSubtitles;
    }

    private async Task<List<ISrtSubtitle>?> TranslateBatchWithRetry(List<ISrtSubtitle> batch, int batchIndex, int totalBatches, string? systemPrompt = null, Language sourceLanguage = Language.Auto, Language targetLanguage = Language.Chinese, int maxRetries = 3)
    {
        #region 准备批次文本

        var batchTexts = new StringBuilder();
        for (int i = 0; i < batch.Count; i++)
        {
            batchTexts.AppendLine($"{i + 1}. {batch[i].Text?.Replace("\r\n", "")}");
        }

        var batchText = batchTexts.ToString();
        progress?.Report($"批次文本长度: {batchText.Length} 字符");

        var maxTokens = Math.Min(batch.Count * 1024, 32000);
        progress?.Report($"设置max_tokens: {maxTokens}");

        #endregion

        #region 重试逻辑

        var lastErrorInfo = new Dictionary<string, int>
        {
            { "expected", 0 },
            { "actual", 0 },
            { "missing", 0 }
        };

        for (int retryCount = 0; retryCount < maxRetries; retryCount++)
        {
            try
            {
                var systemContent = GetEffectiveSystemPrompt(CurrentStyle, systemPrompt, sourceLanguage, targetLanguage);
                var targetLanguageName = GetLanguageDisplayName(targetLanguage);
                var userContent = $"{systemContent}\n请将以下{batch.Count}条字幕翻译成{targetLanguageName}，每条字幕一行，格式为\"序号. 翻译内容\"：\n\n{batchText}";

                if (retryCount > 0 && lastErrorInfo["missing"] > 0)
                {
                    userContent = $"【错误提示】上次你返回了{lastErrorInfo["actual"]}条翻译，但需要{lastErrorInfo["expected"]}条。请重新翻译成{targetLanguageName}，必须返回完整的{batch.Count}条翻译，不能遗漏任何一条！\n\n{userContent}";
                }

                progress?.Report($"发送请求到LM Studio (尝试 {retryCount + 1}/{maxRetries})...");
                if (retryCount > 0)
                {
                    progress?.Report($"提示LM: 上次返回{lastErrorInfo["actual"]}条，需要{lastErrorInfo["expected"]}条");
                }
                progress.SetStatusMessage(userContent);
                var translatedTextBuilder = new StringBuilder();
                await foreach (var chunk in _chatService.SendResponsesStreamAsync(userContent))
                {
                    if (chunk.Success && chunk.ContentDelta != null)
                    {
                        translatedTextBuilder.Append(chunk.ContentDelta);
                    }
                }

                var translatedText = translatedTextBuilder.ToString();

                if (string.IsNullOrEmpty(translatedText))
                {
                    progress?.Error("错误: 翻译结果为空");
                    continue;
                }

                progress?.Report($"收到响应，长度: {translatedText.Length} 字符");
                progress?.Report($"响应内容预览: {translatedText}...");

                #endregion

                #region 解析翻译结果

                var translatedLines = translatedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var translatedMap = new Dictionary<int, string>();

                foreach (var line in translatedLines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.Contains(". "))
                    {
                        var parts = trimmedLine.Split(". ", 2);
                        if (parts.Length == 2 && int.TryParse(parts[0], out var num))
                        {
                            if (num >= 1 && num <= batch.Count)
                            {
                                translatedMap[num] = parts[1].Trim();
                            }
                        }
                    }
                }

                progress?.Report($"成功匹配 {translatedMap.Count} 条翻译");
                progress?.Report($"批次需要翻译 {batch.Count} 条字幕");

                #endregion

                #region 处理翻译结果

                if (translatedMap.Count == batch.Count)
                {
                    progress?.Report($"✓ 批次翻译成功");

                    var translatedSubtitles = new List<ISrtSubtitle>();
                    for (int j = 0; j < batch.Count; j++)
                    {
                        translatedSubtitles.Add(new SrtSubtitle
                        {
                            Index = batch[j].Index,
                            StartTime = batch[j].StartTime,
                            EndTime = batch[j].EndTime,
                            Text = translatedMap[j + 1]
                        });
                    }

                    return translatedSubtitles;
                }
                else
                {
                    progress?.Error($"✗ 错误: 批次{batchIndex}翻译数量不匹配");
                    progress?.Report($"期望 {batch.Count} 条，实际匹配 {translatedMap.Count} 条");

                    lastErrorInfo = new Dictionary<string, int>
                    {
                        { "expected", batch.Count },
                        { "actual", translatedMap.Count },
                        { "missing", batch.Count - translatedMap.Count }
                    };

                    if (retryCount < maxRetries - 1)
                    {
                        progress?.Report($"正在重试...");
                        continue;
                    }
                    else
                    {
                        progress?.Warning($"✗ 批次{batchIndex}连续{maxRetries}次失败，使用部分翻译结果");

                        var partialSubtitles = new List<ISrtSubtitle>();
                        for (int j = 0; j < batch.Count; j++)
                        {
                            partialSubtitles.Add(new SrtSubtitle
                            {
                                Index = batch[j].Index,
                                StartTime = batch[j].StartTime,
                                EndTime = batch[j].EndTime,
                                Text = translatedMap.ContainsKey(j + 1)
                                    ? translatedMap[j + 1]
                                    : $"[翻译失败: 匹配失败，期望{batch.Count}条实际匹配{translatedMap.Count}条]"
                            });
                        }

                        return partialSubtitles;
                    }
                }

                #endregion
            }
            catch (Exception ex)
            {
                progress?.Error($"✗ 错误: {ex.Message}");

                if (retryCount < maxRetries - 1)
                {
                    progress?.Report($"正在重试...");
                    continue;
                }
                else
                {
                    return null;
                }
            }
        }

        return null;
    }

    #region 语言名称映射

    private static readonly Dictionary<Language, string> LanguageDisplayNames = new()
    {
        { Language.Auto, "自动检测" },
        { Language.English, "英语" },
        { Language.Chinese, "中文" },
        { Language.Japanese, "日语" },
        { Language.Korean, "韩语" },
        { Language.Spanish, "西班牙语" },
        { Language.French, "法语" },
        { Language.German, "德语" },
        { Language.Italian, "意大利语" },
        { Language.Portuguese, "葡萄牙语" },
        { Language.Russian, "俄语" },
        { Language.Arabic, "阿拉伯语" },
        { Language.Hindi, "印地语" },
        { Language.Turkish, "土耳其语" },
        { Language.Vietnamese, "越南语" },
        { Language.Thai, "泰语" },
        { Language.Dutch, "荷兰语" },
        { Language.Polish, "波兰语" },
        { Language.Swedish, "瑞典语" },
        { Language.Norwegian, "挪威语" },
        { Language.Danish, "丹麦语" },
        { Language.Finnish, "芬兰语" },
        { Language.Greek, "希腊语" },
        { Language.Czech, "捷克语" },
        { Language.Hungarian, "匈牙利语" },
        { Language.Romanian, "罗马尼亚语" },
        { Language.Bulgarian, "保加利亚语" },
        { Language.Croatian, "克罗地亚语" },
        { Language.Serbian, "塞尔维亚语" },
        { Language.Slovak, "斯洛伐克语" },
        { Language.Slovenian, "斯洛文尼亚语" },
        { Language.Lithuanian, "立陶宛语" },
        { Language.Latvian, "拉脱维亚语" },
        { Language.Estonian, "爱沙尼亚语" },
        { Language.Ukrainian, "乌克兰语" },
        { Language.Hebrew, "希伯来语" },
        { Language.Persian, "波斯语" },
        { Language.Bengali, "孟加拉语" },
        { Language.Tamil, "泰米尔语" },
        { Language.Telugu, "泰卢固语" },
        { Language.Kannada, "卡纳达语" },
        { Language.Malayalam, "马拉雅拉姆语" },
        { Language.Marathi, "马拉地语" },
        { Language.Gujarati, "古吉拉特语" },
        { Language.Punjabi, "旁遮普语" },
        { Language.Urdu, "乌尔都语" },
        { Language.Indonesian, "印度尼西亚语" },
        { Language.Malay, "马来语" },
        { Language.Filipino, "菲律宾语" },
        { Language.Swahili, "斯瓦希里语" },
        { Language.Amharic, "阿姆哈拉语" },
        { Language.Burmese, "缅甸语" },
        { Language.Georgian, "格鲁吉亚语" },
        { Language.Khmer, "高棉语" },
        { Language.Lao, "老挝语" },
        { Language.Mongolian, "蒙古语" },
        { Language.Nepali, "尼泊尔语" },
        { Language.Sinhala, "僧伽罗语" },
        { Language.Albanian, "阿尔巴尼亚语" },
        { Language.Armenian, "亚美尼亚语" },
        { Language.Azerbaijani, "阿塞拜疆语" },
        { Language.Basque, "巴斯克语" },
        { Language.Belarusian, "白俄罗斯语" },
        { Language.Bosnian, "波斯尼亚语" },
        { Language.Catalan, "加泰罗尼亚语" },
        { Language.Esperanto, "世界语" },
        { Language.Galician, "加利西亚语" },
        { Language.Icelandic, "冰岛语" },
        { Language.Irish, "爱尔兰语" },
        { Language.Kazakh, "哈萨克语" },
        { Language.Luxembourgish, "卢森堡语" },
        { Language.Macedonian, "马其顿语" },
        { Language.Moldovan, "摩尔多瓦语" },
        { Language.Montenegrin, "黑山语" },
        { Language.Pashto, "普什图语" },
        { Language.Uzbek, "乌兹别克语" }
    };

    private string GetLanguageDisplayName(Language language)
    {
        if (LanguageDisplayNames.TryGetValue(language, out var name))
        {
            return name;
        }
        return language.ToString();
    }

    #endregion
}
