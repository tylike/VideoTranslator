namespace TextFileFixer.Models;

public enum DiffOperation
{
    Equal,
    Insert,
    Delete,
    Modified
}

public class DiffResultItem
{
    public DiffOperation Operation { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OldLineNumber { get; set; }
    public int NewLineNumber { get; set; }

    public DiffResultItem(DiffOperation operation, string content, int oldLineNumber, int newLineNumber)
    {
        Operation = operation;
        Content = content;
        OldLineNumber = oldLineNumber;
        NewLineNumber = newLineNumber;
    }
}

public class DiffResult
{
    public List<DiffResultItem> Items { get; set; } = new();
    public int InsertCount { get; set; }
    public int DeleteCount { get; set; }
    public int EqualCount { get; set; }
    public int ModifiedCount { get; set; }
}
