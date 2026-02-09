using System.Text.RegularExpressions;
using TextFileFixer.Models;

namespace TextFileFixer.Core;

public class TextPreprocessor
{
    #region Private Fields

    private static readonly char[] PunctuationChars = new[]
    {
        '.', ',', '?', '!', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '—'
    };

    #endregion

    #region Public Methods

    public PreprocessedText Preprocess(string[] lines)
    {
        #region Validation

        if (lines == null)
            throw new ArgumentNullException(nameof(lines));

        #endregion

        #region Initialize Result

        var result = new PreprocessedText();
        int globalWordIndex = 0;

        #endregion

        #region Process Each Line

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var words = SplitIntoWords(line);

            result.LineToWordIndices[lineIndex + 1] = new List<int>();

            #region Process Each Word

            for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
            {
                var word = words[wordIndex];
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                #region Extract Punctuation

                var punctuationMarks = ExtractPunctuation(word);
                var processedWord = RemovePunctuation(word);

                #endregion

                #region Convert to Lowercase

                processedWord = processedWord.ToLowerInvariant();

                #endregion

                #region Create WordLine

                var wordLine = new WordLine(
                    word,
                    processedWord,
                    lineIndex + 1,
                    globalWordIndex
                )
                {
                    PunctuationMarks = punctuationMarks
                };

                #endregion

                result.WordLines.Add(wordLine);
                result.LineToWordIndices[lineIndex + 1].Add(globalWordIndex);
                globalWordIndex++;
            }

            #endregion
        }

        #endregion

        return result;
    }

    public string RestoreOriginalText(WordLine wordLine)
    {
        #region Validation

        if (wordLine == null)
            throw new ArgumentNullException(nameof(wordLine));

        #endregion

        #region Restore Punctuation

        var restored = wordLine.ProcessedText;

        #region Apply Prefix Punctuation

        var prefixPunctuation = wordLine.PunctuationMarks
            .Where(p => p.IsPrefix)
            .OrderBy(p => p.Position)
            .ToList();

        foreach (var punct in prefixPunctuation)
        {
            restored = punct.Punctuation + restored;
        }

        #endregion

        #region Apply Suffix Punctuation

        var suffixPunctuation = wordLine.PunctuationMarks
            .Where(p => !p.IsPrefix)
            .OrderBy(p => p.Position)
            .ToList();

        foreach (var punct in suffixPunctuation)
        {
            restored = restored + punct.Punctuation;
        }

        #endregion

        #endregion

        return restored;
    }

    #endregion

    #region Private Methods

    #region Word Splitting

    private string[] SplitIntoWords(string line)
    {
        #region Split by Whitespace

        var words = Regex.Split(line, @"\s+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToArray();

        #endregion

        return words;
    }

    #endregion

    #region Punctuation Handling

    private List<PunctuationInfo> ExtractPunctuation(string word)
    {
        #region Initialize Result

        var result = new List<PunctuationInfo>();

        #endregion

        #region Extract Prefix Punctuation

        int prefixIndex = 0;
        while (prefixIndex < word.Length && IsPunctuation(word[prefixIndex]))
        {
            result.Add(new PunctuationInfo(
                word[prefixIndex],
                prefixIndex,
                isPrefix: true
            ));
            prefixIndex++;
        }

        #endregion

        #region Extract Suffix Punctuation

        int suffixIndex = word.Length - 1;
        while (suffixIndex >= prefixIndex && IsPunctuation(word[suffixIndex]))
        {
            result.Add(new PunctuationInfo(
                word[suffixIndex],
                suffixIndex,
                isPrefix: false
            ));
            suffixIndex--;
        }

        #endregion

        return result;
    }

    private string RemovePunctuation(string word)
    {
        #region Remove Prefix Punctuation

        int startIndex = 0;
        while (startIndex < word.Length && IsPunctuation(word[startIndex]))
        {
            startIndex++;
        }

        #endregion

        #region Remove Suffix Punctuation

        int endIndex = word.Length - 1;
        while (endIndex >= startIndex && IsPunctuation(word[endIndex]))
        {
            endIndex--;
        }

        #endregion

        #region Extract Clean Word

        if (startIndex > endIndex)
        {
            return string.Empty;
        }

        return word.Substring(startIndex, endIndex - startIndex + 1);

        #endregion
    }

    private bool IsPunctuation(char c)
    {
        return PunctuationChars.Contains(c);
    }

    #endregion

    #endregion
}
