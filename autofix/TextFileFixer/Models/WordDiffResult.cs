namespace TextFileFixer.Models;

public class WordDiffResultItem
{
    public DiffOperation Operation { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string ProcessedText { get; set; } = string.Empty;
    public int OriginalLineNumber { get; set; }
    public int WordIndex { get; set; }
    public List<PunctuationInfo> PunctuationMarks { get; set; } = new();

    public WordDiffResultItem(DiffOperation operation, WordLine wordLine)
    {
        Operation = operation;
        OriginalText = wordLine.OriginalText;
        ProcessedText = wordLine.ProcessedText;
        OriginalLineNumber = wordLine.OriginalLineNumber;
        WordIndex = wordLine.WordIndex;
        PunctuationMarks = wordLine.PunctuationMarks;
    }
}

public class WordDiffResult
{
    public List<WordDiffResultItem> Items { get; set; } = new();
    public int InsertCount { get; set; }
    public int DeleteCount { get; set; }
    public int EqualCount { get; set; }
    public int ModifiedCount { get; set; }
}
