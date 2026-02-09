using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VideoTranslator.SRT.Models;

namespace VideoTranslator.SRT.Services;

public class SrtGenerator
{
    #region 私有字段

    private readonly List<string> _specialTokens = new List<string>
    {
        "[_BEG_]",
        "[_TT_",
        "[_END_]"
    };

    private TtsOptimizationConfig _config;

    #endregion

    #region 构造函数

    public SrtGenerator()
    {
        _config = new TtsOptimizationConfig();
    }

    public SrtGenerator(TtsOptimizationConfig config)
    {
        _config = config ?? new TtsOptimizationConfig();
    }

    #endregion

    #region 公共方法

    public string GenerateSrt(WhisperJsonRoot whisperData)
    {
        if (whisperData?.Transcription == null || !whisperData.Transcription.Any())
        {
            throw new ArgumentException("No transcription data found");
        }

        var srtBuilder = new StringBuilder();
        int subtitleIndex = 1;

        foreach (var segment in whisperData.Transcription)
        {
            if (ShouldSkipSegment(segment))
            {
                continue;
            }

            var srtEntry = GenerateSrtEntry(subtitleIndex, segment);
            srtBuilder.AppendLine(srtEntry);
            srtBuilder.AppendLine();
            subtitleIndex++;
        }

        return srtBuilder.ToString().TrimEnd();
    }

    public string GenerateSrtForTts(WhisperJsonRoot whisperData)
    {
        if (whisperData?.Transcription == null || !whisperData.Transcription.Any())
        {
            throw new ArgumentException("No transcription data found");
        }

        var validSegments = whisperData.Transcription
            .Where(s => !ShouldSkipSegmentForTts(s))
            .ToList();

        var mergedSegments = MergeSegmentsIntoSentences(validSegments);

        if (_config.EnableSmartSplit)
        {
            mergedSegments = SplitLongSegments(mergedSegments);
        }

        mergedSegments = mergedSegments.Where(s => !ShouldSkipSegmentForTts(s)).ToList();

        var srtBuilder = new StringBuilder();
        int subtitleIndex = 1;

        foreach (var segment in mergedSegments)
        {
            var srtEntry = GenerateSrtEntry(subtitleIndex, segment);
            srtBuilder.AppendLine(srtEntry);
            srtBuilder.AppendLine();
            subtitleIndex++;
        }

        return srtBuilder.ToString().TrimEnd();
    }

    private List<TranscriptionSegment> MergeSegmentsIntoSentences(List<TranscriptionSegment> segments)
    {
        var result = new List<TranscriptionSegment>();

        if (segments == null || !segments.Any())
        {
            return result;
        }

        var allTokens = new List<TokenInfo>();
        foreach (var segment in segments)
        {
            if (segment?.Tokens != null)
            {
                allTokens.AddRange(segment.Tokens.Where(t => !IsSpecialToken(t.Text)));
            }
        }

        if (!allTokens.Any())
        {
            return result;
        }

        var sentences = SplitTokensIntoSentences(allTokens);

        foreach (var sentenceTokens in sentences)
        {
            if (sentenceTokens.Any())
            {
                result.Add(CreateSegmentFromTokens(sentenceTokens, sentenceTokens.First(), sentenceTokens.Last()));
            }
        }

        return result;
    }

    private List<List<TokenInfo>> SplitTokensIntoSentences(List<TokenInfo> tokens)
    {
        var sentences = new List<List<TokenInfo>>();
        var currentSentence = new List<TokenInfo>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            currentSentence.Add(token);

            if (IsStrongSentenceEndToken(token.Text, tokens, i))
            {
                sentences.Add(new List<TokenInfo>(currentSentence));
                currentSentence.Clear();
            }
        }

        if (currentSentence.Any())
        {
            sentences.Add(currentSentence);
        }

        return sentences;
    }

    public void SaveSrtToFile(string srtContent, string outputPath)
    {
        if (string.IsNullOrEmpty(srtContent))
        {
            throw new ArgumentNullException(nameof(srtContent));
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            throw new ArgumentNullException(nameof(outputPath));
        }

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, srtContent, System.Text.Encoding.UTF8);
    }

    #endregion

    #region 私有方法 - 基础功能

    private string GenerateSrtEntry(int index, TranscriptionSegment segment)
    {
        var entryBuilder = new StringBuilder();

        entryBuilder.AppendLine(index.ToString());

        string startTime = ConvertToSrtTimeFormat(segment.Timestamps.From);
        string endTime = ConvertToSrtTimeFormat(segment.Timestamps.To);
        entryBuilder.AppendLine($"{startTime} --> {endTime}");

        string cleanedText = CleanText(segment.Text);
        entryBuilder.AppendLine(cleanedText);

        return entryBuilder.ToString().TrimEnd();
    }

    private string ConvertToSrtTimeFormat(string whisperTime)
    {
        if (string.IsNullOrEmpty(whisperTime))
        {
            return "00:00:00,000";
        }

        return whisperTime.Replace(',', ',');
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string cleaned = text.Trim();

        cleaned = cleaned.Replace("  ", " ");

        return cleaned;
    }

    private bool ShouldSkipSegment(TranscriptionSegment segment)
    {
        if (segment == null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(segment.Text))
        {
            return true;
        }

        string text = segment.Text.Trim();

        if (text.Length < 2)
        {
            return true;
        }

        if (_specialTokens.Any(token => text.Contains(token)))
        {
            return true;
        }

        if (segment.Timestamps?.From == segment.Timestamps?.To)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region 私有方法 - TTS 优化

    private bool ShouldSkipSegmentForTts(TranscriptionSegment segment)
    {
        if (segment == null)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(segment.Text))
        {
            return true;
        }

        string text = segment.Text.Trim();

        if (text.Length < _config.MinTextLength)
        {
            return true;
        }

        if (IsOnlyPunctuation(text))
        {
            return true;
        }

        if (_specialTokens.Any(token => text.Contains(token)))
        {
            return true;
        }

        if (_config.ExcludePatterns.Any(pattern => 
            text.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        double duration = GetSegmentDurationMs(segment);
        if (duration <= 0)
        {
            return true;
        }

        return false;
    }

    private bool IsOnlyPunctuation(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        return text.All(c => char.IsPunctuation(c) || char.IsWhiteSpace(c));
    }

    private double GetSegmentDurationMs(TranscriptionSegment segment)
    {
        if (segment?.Offsets == null)
        {
            return 0;
        }

        return segment.Offsets.To - segment.Offsets.From;
    }

    private List<TranscriptionSegment> SplitLongSegments(List<TranscriptionSegment> segments)
    {
        if (segments == null || !segments.Any())
        {
            return segments;
        }

        var result = new List<TranscriptionSegment>();

        foreach (var segment in segments)
        {
            double duration = GetSegmentDurationMs(segment);

            if (duration > _config.SplitThresholdMs)
            {
                var splitSegments = SplitSegment(segment);
                result.AddRange(splitSegments);
            }
            else
            {
                result.Add(segment);
            }
        }

        return result;
    }

    private List<TranscriptionSegment> SplitSegment(TranscriptionSegment segment)
    {
        var splitSegments = new List<TranscriptionSegment>();

        if (segment?.Tokens == null || !segment.Tokens.Any())
        {
            splitSegments.Add(segment);
            return splitSegments;
        }

        var validTokens = segment.Tokens
            .Where(t => !IsSpecialToken(t.Text))
            .ToList();

        if (!validTokens.Any())
        {
            splitSegments.Add(segment);
            return splitSegments;
        }

        double totalDuration = GetSegmentDurationMs(segment);

        if (totalDuration <= _config.MaxDurationMs)
        {
            splitSegments.Add(segment);
            return splitSegments;
        }

        var splitResult = SplitLongSegmentIntelligently(validTokens);

        return splitResult;
    }

    private List<TranscriptionSegment> SplitLongSegmentIntelligently(List<TokenInfo> tokens)
    {
        var splitSegments = new List<TranscriptionSegment>();

        if (!tokens.Any())
        {
            return splitSegments;
        }

        var sentences = SplitIntoSentences(tokens);

        foreach (var sentence in sentences)
        {
            double sentenceDuration = sentence.Last().Offsets.To - sentence.First().Offsets.From;

            if (sentenceDuration <= _config.MaxDurationMs)
            {
                splitSegments.Add(CreateSegmentFromTokens(sentence, sentence.First(), sentence.Last()));
            }
            else
            {
                var splitSentence = SplitLongSentence(sentence);
                splitSegments.AddRange(splitSentence);
            }
        }

        return splitSegments;
    }

    private List<List<TokenInfo>> SplitIntoSentences(List<TokenInfo> tokens)
    {
        var sentences = new List<List<TokenInfo>>();
        var currentSentence = new List<TokenInfo>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            currentSentence.Add(token);

            if (IsStrongSentenceEndToken(token.Text, tokens, i))
            {
                sentences.Add(new List<TokenInfo>(currentSentence));
                currentSentence.Clear();
            }
        }

        if (currentSentence.Any())
        {
            sentences.Add(currentSentence);
        }

        return sentences;
    }

    private List<TranscriptionSegment> SplitLongSentence(List<TokenInfo> sentenceTokens)
    {
        var splitSegments = new List<TranscriptionSegment>();

        if (!sentenceTokens.Any())
        {
            return splitSegments;
        }

        double totalDuration = sentenceTokens.Last().Offsets.To - sentenceTokens.First().Offsets.From;
        int targetSegments = Math.Max(2, (int)Math.Ceiling(totalDuration / _config.OptimalDurationMs));
        double targetDuration = totalDuration / targetSegments;

        var currentTokens = new List<TokenInfo>();
        double currentDuration = 0;

        foreach (var token in sentenceTokens)
        {
            currentTokens.Add(token);
            currentDuration += token.Offsets.To - token.Offsets.From;

            if (currentDuration >= targetDuration && IsWeakSentenceEndToken(token.Text))
            {
                splitSegments.Add(CreateSegmentFromTokens(currentTokens, currentTokens.First(), currentTokens.Last()));
                currentTokens.Clear();
                currentDuration = 0;
            }
        }

        if (currentTokens.Any())
        {
            splitSegments.Add(CreateSegmentFromTokens(currentTokens, currentTokens.First(), currentTokens.Last()));
        }

        return splitSegments;
    }

    private TranscriptionSegment CreateSegmentFromTokens(List<TokenInfo> tokens, TokenInfo firstToken, TokenInfo lastToken)
    {
        var textBuilder = new StringBuilder();
        foreach (var token in tokens)
        {
            textBuilder.Append(token.Text);
        }

        return new TranscriptionSegment
        {
            Text = textBuilder.ToString().Trim(),
            Timestamps = new TimestampInfo
            {
                From = firstToken?.Timestamps?.From ?? "00:00:00,000",
                To = lastToken?.Timestamps?.To ?? "00:00:00,000"
            },
            Offsets = new OffsetInfo
            {
                From = firstToken?.Offsets?.From ?? 0,
                To = lastToken?.Offsets?.To ?? 0
            },
            Tokens = new List<TokenInfo>(tokens)
        };
    }

    private bool IsSpecialToken(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        return _specialTokens.Any(token => text.Contains(token));
    }

    private bool IsStrongSentenceEndToken(string text, List<TokenInfo> tokens, int currentIndex)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        // 检查句号 - 排除数字中的小数点（如 12.1）
        if (text.EndsWith("."))
        {
            // 如果是单个字符的句号，需要检查下一个token
            if (text.Length == 1)
            {
                // 查看下一个token，如果存在且是数字或小写字母开头，则不是句末
                if (currentIndex + 1 < tokens.Count)
                {
                    var nextToken = tokens[currentIndex + 1];
                    var nextText = nextToken.Text.Trim();
                    
                    // 如果下一个token是数字（如 "7"），则可能是小数点（如 "4.7"）
                    if (IsNumeric(nextText))
                        return false;
                    
                    // 如果下一个token是小写字母开头（如 "ai"），则可能是域名（如 "Z.ai"）
                    if (nextText.Length > 0 && char.IsLower(nextText[0]))
                        return false;
                }
                
                return true;
            }

            // 检查是否是纯数字（包括小数）后跟句号（如 2.71.）
            // 这种情况下，句号是句末，不是小数点
            var trimmedText = text.TrimEnd('.');
            if (IsNumeric(trimmedText))
                return true;

            // 检查前面是否是数字（小数点情况）
            if (text.Length >= 2 && char.IsDigit(text[text.Length - 2]))
                return false;

            // 检查是否是缩写（如 Mr., Dr., etc.）
            if (IsCommonAbbreviation(trimmedText))
                return false;

            return true;
        }

        return text.EndsWith("!") || text.EndsWith("?");
    }

    private bool IsNumeric(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // 检查是否是有效的数字（包括小数）
        // 允许的格式：123, 12.3, .123
        bool hasDecimalPoint = false;
        bool hasDigit = false;

        foreach (char c in text)
        {
            if (char.IsDigit(c))
            {
                hasDigit = true;
            }
            else if (c == '.')
            {
                if (hasDecimalPoint)
                    return false; // 多个小数点，不是数字
                hasDecimalPoint = true;
            }
            else
            {
                return false; // 非数字字符
            }
        }

        return hasDigit;
    }

    private bool IsWeakSentenceEndToken(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        // 检查逗号 - 排除数字中的千位分隔符（如 1,000）
        if (text.EndsWith(","))
        {
            // 如果是单个字符的逗号，认为是句内停顿
            if (text.Length == 1)
                return true;

            // 检查前面是否是数字后跟着逗号（千位分隔符情况）
            if (text.Length >= 2)
            {
                // 如果逗号前面有多个数字，可能是千位分隔符
                var beforeComma = text.TrimEnd(',');
                if (beforeComma.Length >= 4 && char.IsDigit(beforeComma[beforeComma.Length - 1]) &&
                    beforeComma.Skip(beforeComma.Length - 4).All(c => char.IsDigit(c)))
                {
                    return false;
                }
                // 如果只是单个数字加逗号，可能是"1,"这种，也算千位分隔符
                if (beforeComma.Length >= 1 && char.IsDigit(beforeComma[beforeComma.Length - 1]))
                {
                    return false;
                }
            }
            return true;
        }

        // 检查分号 - 排除可能出现在数字中的情况（如日期格式）
        if (text.EndsWith(";"))
        {
            if (text.Length == 1)
                return true;

            // 检查是否像日期格式（如 2024;1）
            var beforeSemicolon = text.TrimEnd(';');
            if (beforeSemicolon.Length >= 4 && char.IsDigit(beforeSemicolon[beforeSemicolon.Length - 1]))
            {
                // 如果前面的字符大部分是数字，可能是日期或其他格式
                var digitCount = beforeSemicolon.Count(char.IsDigit);
                if (digitCount >= beforeSemicolon.Length * 0.7)
                    return false;
            }
            return true;
        }

        // 检查冒号 - 排除时间格式（如 12:30）
        if (text.EndsWith(":"))
        {
            if (text.Length == 1)
                return true;

            // 检查是否像时间格式（如 12:）
            var beforeColon = text.TrimEnd(':');
            if (beforeColon.Length >= 2 && char.IsDigit(beforeColon[beforeColon.Length - 1]))
            {
                // 如果前面的字符主要是数字，可能是时间格式
                var digitCount = beforeColon.Count(char.IsDigit);
                if (digitCount >= beforeColon.Length * 0.7)
                    return false;
            }
            return true;
        }

        return false;
    }

    private bool IsCommonAbbreviation(string text)
    {
        var commonAbbreviations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mr", "mrs", "ms", "dr", "prof", "rev", "sr", "jr",
            "gen", "col", "maj", "capt", "lt", "st", "ave", "blvd",
            "rd", "dr", "no", "p", "pp", "vs", "etc", "approx", "jan",
            "feb", "mar", "apr", "jun", "jul", "aug", "sep", "oct", "nov", "dec",
            "mon", "tue", "wed", "thu", "fri", "sat", "sun",
            "co", "ltd", "corp", "inc", "dept", "div", "sec", "ch"
        };

        return commonAbbreviations.Contains(text);
    }

    #endregion

    #region 扩展方法 - 词级别时间戳

    public string GenerateSrtWithWordLevelTimestamps(WhisperJsonRoot whisperData)
    {
        if (whisperData?.Transcription == null || !whisperData.Transcription.Any())
        {
            throw new ArgumentException("No transcription data found");
        }

        var srtBuilder = new StringBuilder();
        int subtitleIndex = 1;

        foreach (var segment in whisperData.Transcription)
        {
            if (ShouldSkipSegment(segment))
            {
                continue;
            }

            var wordSegments = SplitSegmentByWords(segment);

            foreach (var wordSegment in wordSegments)
            {
                var srtEntry = GenerateSrtEntry(subtitleIndex, wordSegment);
                srtBuilder.AppendLine(srtEntry);
                subtitleIndex++;
            }
        }

        return srtBuilder.ToString().TrimEnd();
    }

    private List<TranscriptionSegment> SplitSegmentByWords(TranscriptionSegment segment)
    {
        var wordSegments = new List<TranscriptionSegment>();

        if (segment?.Tokens == null || !segment.Tokens.Any())
        {
            return wordSegments;
        }

        var validTokens = segment.Tokens
            .Where(t => !IsSpecialToken(t.Text))
            .ToList();

        if (!validTokens.Any())
        {
            return wordSegments;
        }

        StringBuilder currentText = new StringBuilder();
        TokenInfo firstToken = null;
        TokenInfo lastToken = null;

        for (int i = 0; i < validTokens.Count; i++)
        {
            var token = validTokens[i];
            if (firstToken == null)
            {
                firstToken = token;
            }

            currentText.Append(token.Text);
            lastToken = token;

            if (IsStrongSentenceEndToken(token.Text, validTokens, i))
            {
                wordSegments.Add(CreateWordSegment(currentText.ToString(), firstToken, lastToken));
                currentText.Clear();
                firstToken = null;
                lastToken = null;
            }
        }

        if (currentText.Length > 0 && firstToken != null && lastToken != null)
        {
            wordSegments.Add(CreateWordSegment(currentText.ToString(), firstToken, lastToken));
        }

        return wordSegments;
    }

    private TranscriptionSegment CreateWordSegment(string text, TokenInfo firstToken, TokenInfo lastToken)
    {
        return new TranscriptionSegment
        {
            Text = text.Trim(),
            Timestamps = new TimestampInfo
            {
                From = firstToken?.Timestamps?.From ?? "00:00:00,000",
                To = lastToken?.Timestamps?.To ?? "00:00:00,000"
            },
            Offsets = new OffsetInfo
            {
                From = firstToken?.Offsets?.From ?? 0,
                To = lastToken?.Offsets?.To ?? 0
            }
        };
    }

    #endregion
}
