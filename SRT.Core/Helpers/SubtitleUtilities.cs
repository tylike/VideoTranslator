using VT.Core;

namespace VideoTranslator.SRT.Core.Helpers;

public static class SubtitleUtilities
{
    #region HTML标签处理

    public static string RemoveHtmlTags(string text, bool alsoTrim = false)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new System.Text.StringBuilder();
        var tagOn = false;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '<')
            {
                tagOn = true;
            }
            else if (c == '>')
            {
                tagOn = false;
            }
            else if (!tagOn)
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();
        if (alsoTrim)
        {
            result = result.Trim();
        }

        return result;
    }

    #endregion

    #region CJK字符检测

    public static bool IsCjk(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF) ||
               (c >= 0x3400 && c <= 0x4DBF) ||
               (c >= 0x20000 && c <= 0x2A6DF) ||
               (c >= 0x2A700 && c <= 0x2B73F) ||
               (c >= 0x2B740 && c <= 0x2B81F) ||
               (c >= 0x2B820 && c <= 0x2CEAF) ||
               (c >= 0xF900 && c <= 0xFAFF) ||
               (c >= 0x2F800 && c <= 0x2FA1F);
    }

    #endregion

    #region 合并条件判断

    public static bool QualifiesForMerge(ISrtSubtitle p, ISrtSubtitle next, 
        double maximumMillisecondsBetweenLines, int maximumTotalLength, bool onlyContinuationLines)
    {
        if (p?.Text != null && next?.Text != null)
        {
            var s = RemoveHtmlTags(p.Text.Trim(), true);
            var nextText = RemoveHtmlTags(next.Text.Trim(), true);

            if (s.Length + nextText.Length < maximumTotalLength && 
                next.StartTime.TotalMilliseconds - p.EndTime.TotalMilliseconds < maximumMillisecondsBetweenLines)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return true;
                }

                var isLineContinuation = s.EndsWith("...", StringComparison.Ordinal) ||
                                              s.EndsWith(",") ||
                                              IsCjk(s[s.Length - 1]);

                if (s.EndsWith('♪') || nextText.StartsWith('♪'))
                {
                    return false;
                }

                return isLineContinuation;
            }
        }
        return false;
    }

    #endregion

    #region 合并下一个字幕

    public static void MergeNextIntoP(string language, ISrtSubtitle p, ISrtSubtitle next)
    {
        var startTag = GetStartTag(p.Text);
        var endTag = GetEndTag(p.Text);
        var nextStartTag = GetStartTag(next.Text);
        var nextEndTag = GetEndTag(next.Text);

        if (startTag == nextStartTag && endTag == nextEndTag)
        {
            var s1 = p.Text.Trim();
            s1 = s1.Substring(0, s1.Length - endTag.Length);
            var s2 = next.Text.Trim();
            s2 = s2.Substring(nextStartTag.Length);
            p.Text = AutoBreakLine(s1 + Environment.NewLine + s2, language);
        }
        else
        {
            p.Text = AutoBreakLine(p.Text + Environment.NewLine + next.Text, language);
        }

        p.EndTime = next.EndTime;

        if (IsNonStandardLineTerminationLanguage(language))
        {
            p.Text = p.Text.Replace("\r", "").Replace("\n", "").Replace(" ", "");
        }
    }

    private static string GetStartTag(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        text = text.Trim();
        if (!text.StartsWith('<'))
        {
            return string.Empty;
        }

        var startTag = string.Empty;
        var end = text.IndexOf('>');
        if (end > 0 && end < 25)
        {
            startTag = text.Substring(0, end + 1);
        }

        return startTag;
    }

    private static string GetEndTag(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        text = text.Trim();
        if (!text.EndsWith('>'))
        {
            return string.Empty;
        }

        var endTag = string.Empty;
        var start = text.LastIndexOf("</", StringComparison.Ordinal);
        if (start > 0 && start >= text.Length - 8)
        {
            endTag = text.Substring(start);
        }
        return endTag;
    }

    #endregion

    #region 自动换行

    public static string AutoBreakLine(string text, string language)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var maxLength = GetMaxLineLength(language);
        
        if (text.Length <= maxLength)
        {
            return text;
        }

        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length == 0)
            {
                currentLine.Append(word);
            }
            else if (currentLine.Length + 1 + word.Length <= maxLength)
            {
                currentLine.Append(' ').Append(word);
            }
            else
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentLine.Append(word);
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static int GetMaxLineLength(string language)
    {
        return language switch
        {
            "jp" => 30,
            "cn" => 30,
            "yue" => 30,
            _ => 43
        };
    }

    #endregion

    #region 语言判断

    public static bool IsNonStandardLineTerminationLanguage(string language)
    {
        return language == "jp" || language == "cn" || language == "yue";
    }

    #endregion

    #region 字符计数

    public static int CountCharacters(string text, bool countHtml)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        if (countHtml)
        {
            return text.Length;
        }

        return RemoveHtmlTags(text, false).Length;
    }

    #endregion
}
