namespace TextFileFixer.Models;

public class WordLine
{
    public string OriginalText { get; set; } = string.Empty;
    public string ProcessedText { get; set; } = string.Empty;
    public int OriginalLineNumber { get; set; }
    public int WordIndex { get; set; }
    public List<PunctuationInfo> PunctuationMarks { get; set; } = new();

    public WordLine(string originalText, string processedText, int originalLineNumber, int wordIndex)
    {
        OriginalText = originalText;
        ProcessedText = processedText;
        OriginalLineNumber = originalLineNumber;
        WordIndex = wordIndex;
    }
}

public class PunctuationInfo
{
    public char Punctuation { get; set; }
    public int Position { get; set; }
    public bool IsPrefix { get; set; }

    public PunctuationInfo(char punctuation, int position, bool isPrefix)
    {
        Punctuation = punctuation;
        Position = position;
        IsPrefix = isPrefix;
    }
}

public class PreprocessedText
{
    public List<WordLine> WordLines { get; set; } = new();
    public Dictionary<int, List<int>> LineToWordIndices { get; set; } = new();
}
