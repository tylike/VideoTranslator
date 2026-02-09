namespace TextFileFixer.Models;

public class MergedWordLine
{
    public string Content { get; set; } = string.Empty;
    public int OldLineNumber { get; set; }
    public DiffOperation Operation { get; set; }
    public bool IsFromNewFile { get; set; }

    public MergedWordLine(string content, int oldLineNumber, DiffOperation operation, bool isFromNewFile)
    {
        Content = content;
        OldLineNumber = oldLineNumber;
        Operation = operation;
        IsFromNewFile = isFromNewFile;
    }
}

public class MergedDiffResult
{
    public List<MergedWordLine> Lines { get; set; } = new();
    public int TotalLines { get; set; }
    public Dictionary<int, List<MergedWordLine>> LineGroups { get; set; } = new();
}
