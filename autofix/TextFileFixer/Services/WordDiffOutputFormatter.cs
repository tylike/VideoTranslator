using TextFileFixer.Models;

namespace TextFileFixer.Services;

public class WordDiffOutputFormatter
{
    #region Public Methods

    public string FormatWordDiffResult(WordDiffResult diffResult, string oldFileName, string newFileName)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Add Header

        output.AppendLine("=".PadRight(80, '='));
        output.AppendLine($"WORD-LEVEL DIFF: {oldFileName} -> {newFileName}");
        output.AppendLine("=".PadRight(80, '='));

        #endregion

        #region Add Summary

        output.AppendLine();
        output.AppendLine("Summary:");
        output.AppendLine($"  Words Added:    {diffResult.InsertCount}");
        output.AppendLine($"  Words Removed:  {diffResult.DeleteCount}");
        output.AppendLine($"  Unchanged:      {diffResult.EqualCount}");
        output.AppendLine($"  Modified:       {diffResult.ModifiedCount}");
        output.AppendLine($"  Total Changes:  {diffResult.InsertCount + diffResult.DeleteCount + diffResult.ModifiedCount}");
        output.AppendLine();

        #endregion

        #region Add Diff Details

        output.AppendLine("-".PadRight(80, '-'));
        output.AppendLine("Details (Original Line # | Original Text | Processed Text):");
        output.AppendLine("-".PadRight(80, '-'));
        output.AppendLine();

        foreach (var item in diffResult.Items)
        {
            #region Format Based on Operation

            switch (item.Operation)
            {
                case DiffOperation.Equal:
                    output.AppendLine($"  [Line {item.OriginalLineNumber}] {item.OriginalText}");
                    break;

                case DiffOperation.Insert:
                    output.AppendLine($"+ [Line {item.OriginalLineNumber}] {item.OriginalText}");
                    break;

                case DiffOperation.Delete:
                    output.AppendLine($"- [Line {item.OriginalLineNumber}] {item.OriginalText}");
                    break;

                case DiffOperation.Modified:
                    output.AppendLine($"~ [Line {item.OriginalLineNumber}] {item.OriginalText}");
                    break;
            }

            #endregion
        }

        #endregion

        #region Add Footer

        output.AppendLine();
        output.AppendLine("=".PadRight(80, '='));

        #endregion

        return output.ToString();
    }

    public string FormatWordDiffResultWithProcessed(WordDiffResult diffResult, string oldFileName, string newFileName)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Add Header

        output.AppendLine("=".PadRight(80, '='));
        output.AppendLine($"WORD-LEVEL DIFF (Detailed): {oldFileName} -> {newFileName}");
        output.AppendLine("=".PadRight(80, '='));

        #endregion

        #region Add Summary

        output.AppendLine();
        output.AppendLine("Summary:");
        output.AppendLine($"  Words Added:    {diffResult.InsertCount}");
        output.AppendLine($"  Words Removed:  {diffResult.DeleteCount}");
        output.AppendLine($"  Unchanged:      {diffResult.EqualCount}");
        output.AppendLine($"  Modified:       {diffResult.ModifiedCount}");
        output.AppendLine($"  Total Changes:  {diffResult.InsertCount + diffResult.DeleteCount + diffResult.ModifiedCount}");
        output.AppendLine();

        #endregion

        #region Add Diff Details

        output.AppendLine("-".PadRight(80, '-'));
        output.AppendLine("Details (Original Line # | Original Text | Processed Text):");
        output.AppendLine("-".PadRight(80, '-'));
        output.AppendLine();

        foreach (var item in diffResult.Items)
        {
            #region Format Based on Operation

            switch (item.Operation)
            {
                case DiffOperation.Equal:
                    output.AppendLine($"  [Line {item.OriginalLineNumber}] {item.OriginalText}");
                    output.AppendLine($"    -> Processed: {item.ProcessedText}");
                    break;

                case DiffOperation.Insert:
                    output.AppendLine($"+ [Line {item.OriginalLineNumber}] {item.OriginalText}");
                    output.AppendLine($"    -> Processed: {item.ProcessedText}");
                    break;

                case DiffOperation.Delete:
                    output.AppendLine($"- [Line {item.OriginalLineNumber}] {item.OriginalText}");
                    output.AppendLine($"    -> Processed: {item.ProcessedText}");
                    break;

                case DiffOperation.Modified:
                    output.AppendLine($"~ [Line {item.OriginalLineNumber}] {item.OriginalText}");
                    output.AppendLine($"    -> Processed: {item.ProcessedText}");
                    break;
            }

            #endregion
        }

        #endregion

        #region Add Footer

        output.AppendLine();
        output.AppendLine("=".PadRight(80, '='));

        #endregion

        return output.ToString();
    }

    public void WriteWordDiffToFile(WordDiffResult diffResult, string oldFileName, string newFileName, string outputPath)
    {
        #region Format Diff

        var formattedDiff = FormatWordDiffResultWithProcessed(diffResult, oldFileName, newFileName);

        #endregion

        #region Write to File

        File.WriteAllText(outputPath, formattedDiff);

        #endregion
    }

    #region Side-by-Side Display

    public string FormatWordDiffSideBySide(WordDiffResult diffResult, string oldFileName, string newFileName)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Add Header

        output.AppendLine("=".PadRight(120, '='));
        output.AppendLine($"WORD-LEVEL DIFF (Side-by-Side): {oldFileName} -> {newFileName}");
        output.AppendLine("=".PadRight(120, '='));

        #endregion

        #region Add Summary

        output.AppendLine();
        output.AppendLine("Summary:");
        output.AppendLine($"  Words Added:    {diffResult.InsertCount}");
        output.AppendLine($"  Words Removed:  {diffResult.DeleteCount}");
        output.AppendLine($"  Unchanged:      {diffResult.EqualCount}");
        output.AppendLine($"  Modified:       {diffResult.ModifiedCount}");
        output.AppendLine($"  Total Changes:  {diffResult.InsertCount + diffResult.DeleteCount + diffResult.ModifiedCount}");
        output.AppendLine();

        #endregion

        #region Add Column Headers

        output.AppendLine("-".PadRight(120, '-'));
        output.AppendLine($"{"Status",-8} | {"Old File",-40} | {"New File",-40} | {"Line",-5}");
        output.AppendLine("-".PadRight(120, '-'));

        #endregion

        #region Process Diff Items

        foreach (var item in diffResult.Items)
        {
            #region Format Based on Operation

            string oldWord;
            string newWord;
            string lineInfo;

            switch (item.Operation)
            {
                case DiffOperation.Equal:
                    oldWord = item.OriginalText;
                    newWord = item.OriginalText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Insert:
                    oldWord = "";
                    newWord = item.OriginalText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Delete:
                    oldWord = item.OriginalText;
                    newWord = "";
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Modified:
                    oldWord = item.OriginalText;
                    newWord = item.OriginalText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                default:
                    oldWord = "";
                    newWord = "";
                    lineInfo = "";
                    break;
            }

            #endregion

            #region Color Coding (using symbols)

            string statusSymbol = item.Operation switch
            {
                DiffOperation.Equal => "  ✓",
                DiffOperation.Insert => "  +",
                DiffOperation.Delete => "  -",
                DiffOperation.Modified => "  ~",
                _ => "  ?"
            };

            #endregion

            #region Output Line

            output.AppendLine($"{statusSymbol,-8} | {oldWord,-40} | {newWord,-40} | {lineInfo,-5}");

            #endregion
        }

        #endregion

        #region Add Footer

        output.AppendLine("-".PadRight(120, '-'));
        output.AppendLine("=".PadRight(120, '='));

        #endregion

        return output.ToString();
    }

    public string FormatWordDiffSideBySideWithProcessed(WordDiffResult diffResult, string oldFileName, string newFileName)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Add Header

        output.AppendLine("=".PadRight(160, '='));
        output.AppendLine($"WORD-LEVEL DIFF (Side-by-Side with Processed): {oldFileName} -> {newFileName}");
        output.AppendLine("=".PadRight(160, '='));

        #endregion

        #region Add Summary

        output.AppendLine();
        output.AppendLine("Summary:");
        output.AppendLine($"  Words Added:    {diffResult.InsertCount}");
        output.AppendLine($"  Words Removed:  {diffResult.DeleteCount}");
        output.AppendLine($"  Unchanged:      {diffResult.EqualCount}");
        output.AppendLine($"  Modified:       {diffResult.ModifiedCount}");
        output.AppendLine($"  Total Changes:  {diffResult.InsertCount + diffResult.DeleteCount + diffResult.ModifiedCount}");
        output.AppendLine();

        #endregion

        #region Add Column Headers

        output.AppendLine("-".PadRight(160, '-'));
        output.AppendLine($"{"Status",-8} | {"Old File",-30} | {"Old Proc",-20} | {"New File",-30} | {"New Proc",-20} | {"Line",-5}");
        output.AppendLine("-".PadRight(160, '-'));

        #endregion

        #region Process Diff Items

        foreach (var item in diffResult.Items)
        {
            #region Format Based on Operation

            string oldWord;
            string oldProcessed;
            string newWord;
            string newProcessed;
            string lineInfo;

            switch (item.Operation)
            {
                case DiffOperation.Equal:
                    oldWord = item.OriginalText;
                    oldProcessed = item.ProcessedText;
                    newWord = item.OriginalText;
                    newProcessed = item.ProcessedText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Insert:
                    oldWord = "";
                    oldProcessed = "";
                    newWord = item.OriginalText;
                    newProcessed = item.ProcessedText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Delete:
                    oldWord = item.OriginalText;
                    oldProcessed = item.ProcessedText;
                    newWord = "";
                    newProcessed = "";
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Modified:
                    oldWord = item.OriginalText;
                    oldProcessed = item.ProcessedText;
                    newWord = item.OriginalText;
                    newProcessed = item.ProcessedText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                default:
                    oldWord = "";
                    oldProcessed = "";
                    newWord = "";
                    newProcessed = "";
                    lineInfo = "";
                    break;
            }

            #endregion

            #region Color Coding (using symbols)

            string statusSymbol = item.Operation switch
            {
                DiffOperation.Equal => "  ✓",
                DiffOperation.Insert => "  +",
                DiffOperation.Delete => "  -",
                DiffOperation.Modified => "  ~",
                _ => "  ?"
            };

            #endregion

            #region Output Line

            output.AppendLine($"{statusSymbol,-8} | {oldWord,-30} | {oldProcessed,-20} | {newWord,-30} | {newProcessed,-20} | {lineInfo,-5}");

            #endregion
        }

        #endregion

        #region Add Footer

        output.AppendLine("-".PadRight(160, '-'));
        output.AppendLine("=".PadRight(160, '='));

        #endregion

        return output.ToString();
    }

    #region Compact Display

    public string FormatWordDiffCompact(WordDiffResult diffResult, string oldFileName, string newFileName)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Add Header

        output.AppendLine("=".PadRight(100, '='));
        output.AppendLine($"WORD-LEVEL DIFF (Compact): {oldFileName} -> {newFileName}");
        output.AppendLine("=".PadRight(100, '='));

        #endregion

        #region Add Summary

        output.AppendLine();
        output.AppendLine("Summary:");
        output.AppendLine($"  Words Added:    {diffResult.InsertCount}");
        output.AppendLine($"  Words Removed:  {diffResult.DeleteCount}");
        output.AppendLine($"  Unchanged:      {diffResult.EqualCount}");
        output.AppendLine($"  Modified:       {diffResult.ModifiedCount}");
        output.AppendLine($"  Total Changes:  {diffResult.InsertCount + diffResult.DeleteCount + diffResult.ModifiedCount}");
        output.AppendLine();

        #endregion

        #region Add Column Headers

        output.AppendLine("-".PadRight(100, '-'));
        output.AppendLine($"{"Status",-6} | {"Old File",-35} | {"New File",-35} | {"Line",-4}");
        output.AppendLine("-".PadRight(100, '-'));

        #endregion

        #region Process Diff Items

        foreach (var item in diffResult.Items)
        {
            #region Format Based on Operation

            string oldWord;
            string newWord;
            string lineInfo;

            switch (item.Operation)
            {
                case DiffOperation.Equal:
                    oldWord = item.OriginalText;
                    newWord = item.OriginalText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Insert:
                    oldWord = "";
                    newWord = item.OriginalText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Delete:
                    oldWord = item.OriginalText;
                    newWord = "";
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                case DiffOperation.Modified:
                    oldWord = item.OriginalText;
                    newWord = item.OriginalText;
                    lineInfo = item.OriginalLineNumber.ToString();
                    break;

                default:
                    oldWord = "";
                    newWord = "";
                    lineInfo = "";
                    break;
            }

            #endregion

            #region Color Coding (using symbols)

            string statusSymbol = item.Operation switch
            {
                DiffOperation.Equal => "  ✓",
                DiffOperation.Insert => "  +",
                DiffOperation.Delete => "  -",
                DiffOperation.Modified => "  ~",
                _ => "  ?"
            };

            #endregion

            #region Output Line

            output.AppendLine($"{statusSymbol,-6} | {oldWord,-35} | {newWord,-35} | {lineInfo,-4}");

            #endregion
        }

        #endregion

        #region Add Footer

        output.AppendLine("-".PadRight(100, '-'));
        output.AppendLine("=".PadRight(100, '='));

        #endregion

        return output.ToString();
    }

    #endregion

    #endregion

    #endregion
}
