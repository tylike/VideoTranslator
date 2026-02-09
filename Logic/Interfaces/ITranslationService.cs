using System.Text;
using System.Text.Json;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.SRT.Core.Models;
using VideoTranslator.Services;
using VT.Core;

namespace VideoTranslator.Interfaces;

public interface ITranslationService
{
    Task<List<ISrtSubtitle>> TranslateSubtitlesAsync(
        IEnumerable<ISrtSubtitle> subtitles,
        TranslationApi api,
        string? systemPrompt,
        int? batchSize,
        Language targetLanguage,
        Language sourceLanguage);
}

public enum TranslationApi
{
    LMStudio,
    Google,
    Bing,
    DeepL
}
public class TranslationService : ServiceBase, ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly LMStudioTranslationService _lmStudioService;

    public TranslationService() : base()
    {
        _httpClient = new HttpClient();
        _lmStudioService = new LMStudioTranslationService();
    }

    public async Task<List<ISrtSubtitle>> TranslateSubtitlesAsync(
        IEnumerable<ISrtSubtitle> subtitles,
        TranslationApi api,
        string? systemPrompt,
        int? batchSize,
        Language targetLanguage,
        Language sourceLanguage)
    {
        if (api == TranslationApi.LMStudio)
        {
            return await _lmStudioService.TranslateSubtitlesAsync(subtitles, api, systemPrompt, batchSize, targetLanguage, sourceLanguage);
        }

        progress?.Report("正在准备字幕翻译...");

        progress?.Report($"[TranslationService] 字幕翻译工具");
        progress?.Report(new string('=', 60));
        progress?.Report($"翻译API: {api}");
        progress?.Report($"共 {subtitles.Count()} 条字幕待翻译");

        var translatedSubtitles = new List<ISrtSubtitle>();
        var actualBatchSize = batchSize ?? 1;
        var totalBatches = (int)Math.Ceiling((double)subtitles.Count() / actualBatchSize);

        for (int i = 0; i < subtitles.Count(); i += actualBatchSize)
        {
            var batchEnd = Math.Min(i + actualBatchSize, subtitles.Count());
            var batch = subtitles.Skip(i).Take(batchEnd - i).ToList();
            var batchIndex = i / actualBatchSize + 1;
            var progressPercentage = (double)batchEnd / subtitles.Count() * 100;

            progress?.Report($"\n[{i + 1}-{batchEnd}/{subtitles.Count()}] 翻译中...");
            progress?.Report($"原文: {batch[0].Text[..Math.Min(100, batch[0].Text.Length)]}");

            progress?.ReportProgress(progressPercentage, $"正在翻译字幕 [{i + 1}-{batchEnd}/{subtitles.Count()}]...");

            var translatedBatch = await TranslateBatchAsync(batch, api, targetLanguage, sourceLanguage);

            if (translatedBatch != null)
            {
                translatedSubtitles.AddRange(translatedBatch);
                progress?.Report($"译文: {translatedBatch[0].Text[..Math.Min(100, translatedBatch[0].Text.Length)]}");
            }
            else
            {
                progress?.Warning($"✗ 翻译失败，保留原文");
                translatedSubtitles.Add(new SrtSubtitle
                {
                    Index = batch[0].Index,
                    StartTime = batch[0].StartTime,
                    EndTime = batch[0].EndTime,
                    Text = batch[0].Text
                });
            }

            if (batchEnd % 10 == 0 || batchEnd == subtitles.Count())
            {
                progress?.Report($"\n进度: {batchEnd}/{subtitles.Count()} ({progressPercentage:F1}%)");
            }
        }

        progress?.Report("字幕翻译完成！");

        progress?.Report($"\n{new string('=', 60)}");
        progress?.Report($"翻译完成!");
        progress?.Report($"总字幕数: {subtitles.Count()}");

        return translatedSubtitles;
    }

    private async Task<List<ISrtSubtitle>?> TranslateBatchAsync(List<ISrtSubtitle> batch, TranslationApi api, Language targetLanguage, Language sourceLanguage)
    {
        try
        {
            var batchTexts = batch.Select(s => s.Text).ToList();
            var translatedTexts = await TranslateTextBatchAsync(batchTexts, api, targetLanguage, sourceLanguage);

            if (translatedTexts != null && translatedTexts.Count == batch.Count)
            {
                var result = new List<ISrtSubtitle>();
                for (int i = 0; i < batch.Count; i++)
                {
                    result.Add(new SrtSubtitle
                    {
                        Index = batch[i].Index,
                        StartTime = batch[i].StartTime,
                        EndTime = batch[i].EndTime,
                        Text = translatedTexts[i]
                    });
                }
                return result;
            }
            return null;
        }
        catch (Exception ex)
        {
            progress?.Error($"批量翻译失败: {ex.Message}");
            return null;
        }
    }

    private async Task<List<string>?> TranslateTextBatchAsync(List<string> texts, TranslationApi api, Language targetLanguage, Language sourceLanguage)
    {
        try
        {
            return api switch
            {
                TranslationApi.Google => await TranslateTextBatchGoogleAsync(texts, targetLanguage, sourceLanguage),
                TranslationApi.Bing => await TranslateTextBatchBingAsync(texts, targetLanguage, sourceLanguage),
                TranslationApi.DeepL => await TranslateTextBatchDeepLAsync(texts, targetLanguage, sourceLanguage),
                _ => await TranslateTextBatchGoogleAsync(texts, targetLanguage, sourceLanguage)
            };
        }
        catch (Exception ex)
        {
            progress?.Error($"批量翻译失败: {ex.Message}{ex.InnerException?.Message}");
            return null;
        }
    }

    private async Task<List<string>?> TranslateTextBatchGoogleAsync(List<string> texts, Language targetLanguage, Language sourceLanguage)
    {
        const int maxBatchSize = 50;
        const int delayMs = 100;
        var results = new List<string>();

        for (int i = 0; i < texts.Count; i += maxBatchSize)
        {
            var batchEnd = Math.Min(i + maxBatchSize, texts.Count);
            var batch = texts.Skip(i).Take(batchEnd - i).ToList();

            var batchResults = await TranslateTextBatchInternalAsync(batch, targetLanguage, sourceLanguage);
            if (batchResults != null)
            {
                results.AddRange(batchResults);
            }
            else
            {
                return null;
            }

            if (batchEnd < texts.Count)
            {
                await Task.Delay(delayMs);
            }
        }

        return results;
    }

    private async Task<List<string>?> TranslateTextBatchInternalAsync(List<string> texts, Language targetLanguage, Language sourceLanguage)
    {
        var combinedText = string.Join("\n", texts);
        var targetLangCode = GetGoogleLanguageCode(targetLanguage);
        var sourceLangCode = GetGoogleLanguageCode(sourceLanguage);
        var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLangCode}&tl={targetLangCode}&dt=t&q={Uri.EscapeDataString(combinedText)}";
        var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);

            if (result.ValueKind == JsonValueKind.Array && result.GetArrayLength() > 0)
            {
                var translated = new StringBuilder();
                foreach (var item in result[0].EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() > 0)
                    {
                        translated.Append(item[0].GetString());
                    }
                }
                var translatedTexts = translated.ToString().Split('\n');
                if (translatedTexts.Length == texts.Count)
                {
                    return translatedTexts.ToList();
                }
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            progress?.Error($"Google批量翻译失败: HTTP {response.StatusCode} - {errorContent}");
        }

        return null;
    }



    private async Task<List<string>?> TranslateTextBatchBingAsync(List<string> texts, Language targetLanguage, Language sourceLanguage)
    {
        progress?.Warning($"警告: Bing翻译暂未实现，使用Google翻译");
        return await TranslateTextBatchGoogleAsync(texts, targetLanguage, sourceLanguage);
    }



    private async Task<List<string>?> TranslateTextBatchDeepLAsync(List<string> texts, Language targetLanguage, Language sourceLanguage)
    {
        progress?.Report($"警告: DeepL翻译暂未实现，使用Google翻译");
        return await TranslateTextBatchGoogleAsync(texts, targetLanguage, sourceLanguage);
    }

    #region 语言代码映射

    private static readonly Dictionary<Language, string> GoogleLanguageCodes = new()
    {
        { Language.Auto, "auto" },
        { Language.English, "en" },
        { Language.Chinese, "zh-CN" },
        { Language.Japanese, "ja" },
        { Language.Korean, "ko" },
        { Language.Spanish, "es" },
        { Language.French, "fr" },
        { Language.German, "de" },
        { Language.Italian, "it" },
        { Language.Portuguese, "pt" },
        { Language.Russian, "ru" },
        { Language.Arabic, "ar" },
        { Language.Hindi, "hi" },
        { Language.Turkish, "tr" },
        { Language.Vietnamese, "vi" },
        { Language.Thai, "th" },
        { Language.Dutch, "nl" },
        { Language.Polish, "pl" },
        { Language.Swedish, "sv" },
        { Language.Norwegian, "no" },
        { Language.Danish, "da" },
        { Language.Finnish, "fi" },
        { Language.Greek, "el" },
        { Language.Czech, "cs" },
        { Language.Hungarian, "hu" },
        { Language.Romanian, "ro" },
        { Language.Bulgarian, "bg" },
        { Language.Croatian, "hr" },
        { Language.Serbian, "sr" },
        { Language.Slovak, "sk" },
        { Language.Slovenian, "sl" },
        { Language.Lithuanian, "lt" },
        { Language.Latvian, "lv" },
        { Language.Estonian, "et" },
        { Language.Ukrainian, "uk" },
        { Language.Hebrew, "iw" },
        { Language.Persian, "fa" },
        { Language.Bengali, "bn" },
        { Language.Tamil, "ta" },
        { Language.Telugu, "te" },
        { Language.Kannada, "kn" },
        { Language.Malayalam, "ml" },
        { Language.Marathi, "mr" },
        { Language.Gujarati, "gu" },
        { Language.Punjabi, "pa" },
        { Language.Urdu, "ur" },
        { Language.Indonesian, "id" },
        { Language.Malay, "ms" },
        { Language.Filipino, "tl" },
        { Language.Swahili, "sw" },
        { Language.Amharic, "am" },
        { Language.Burmese, "my" },
        { Language.Georgian, "ka" },
        { Language.Khmer, "km" },
        { Language.Lao, "lo" },
        { Language.Mongolian, "mn" },
        { Language.Nepali, "ne" },
        { Language.Sinhala, "si" },
        { Language.Albanian, "sq" },
        { Language.Armenian, "hy" },
        { Language.Azerbaijani, "az" },
        { Language.Basque, "eu" },
        { Language.Belarusian, "be" },
        { Language.Bosnian, "bs" },
        { Language.Catalan, "ca" },
        { Language.Esperanto, "eo" },
        { Language.Galician, "gl" },
        { Language.Icelandic, "is" },
        { Language.Irish, "ga" },
        { Language.Kazakh, "kk" },
        { Language.Luxembourgish, "lb" },
        { Language.Macedonian, "mk" },
        { Language.Moldovan, "ro" },
        { Language.Montenegrin, "sr" },
        { Language.Pashto, "ps" },
        { Language.Uzbek, "uz" }
    };

    private string GetGoogleLanguageCode(Language language)
    {
        if (GoogleLanguageCodes.TryGetValue(language, out var code))
        {
            return code;
        }
        return "auto";
    }

    #endregion
}