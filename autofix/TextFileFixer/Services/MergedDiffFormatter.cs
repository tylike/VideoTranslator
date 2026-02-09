using TextFileFixer.Models;

namespace TextFileFixer.Services;

public class MergedDiffFormatter
{
    #region Public Methods

    public string FormatMergedDiff(MergedDiffResult mergedResult, string oldFileName, string newFileName)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Add Header

        output.AppendLine("=".PadRight(120, '='));
        output.AppendLine($"MERGED DIFF: {oldFileName} (old) -> {newFileName} (new)");
        output.AppendLine("=".PadRight(120, '='));

        #endregion

        #region Add Summary

        output.AppendLine();
        output.AppendLine("Summary:");
        output.AppendLine($"  Total Lines: {mergedResult.TotalLines}");
        output.AppendLine($"  Total Words: {mergedResult.Lines.Count}");
        output.AppendLine();

        #endregion

        #region Add Column Headers

        output.AppendLine("-".PadRight(120, '-'));
        output.AppendLine($"{"Line",-5} | {"Status",-8} | {"Content",-90}");
        output.AppendLine("-".PadRight(120, '-'));

        #endregion

        #region Process Line Groups

        foreach (var lineGroup in mergedResult.LineGroups.OrderBy(x => x.Key))
        {
            var lineNumber = lineGroup.Key;
            var words = lineGroup.Value;

            #region Output Each Word in Line

            foreach (var word in words)
            {
                #region Format Status

                string statusSymbol = word.Operation switch
                {
                    DiffOperation.Equal => "  ✓",
                    DiffOperation.Insert => "  +",
                    DiffOperation.Delete => "  -",
                    DiffOperation.Modified => "  ~",
                    _ => "  ?"
                };

                #endregion

                #region Output Line

                output.AppendLine($"{lineNumber,-5} | {statusSymbol,-8} | {word.Content,-90}");

                #endregion
            }

            #endregion
        }

        #endregion

        #region Add Footer

        output.AppendLine("-".PadRight(120, '-'));
        output.AppendLine("=".PadRight(120, '='));

        #endregion

        return output.ToString();
    }

    public string FormatMergedDiffCompact(MergedDiffResult mergedResult, string oldFileName, string newFileName)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Add Header

        output.AppendLine("=".PadRight(100, '='));
        output.AppendLine($"MERGED DIFF (Compact): {oldFileName} (old) -> {newFileName} (new)");
        output.AppendLine("=".PadRight(100, '='));

        #endregion

        #region Add Summary

        output.AppendLine();
        output.AppendLine("Summary:");
        output.AppendLine($"  Total Lines: {mergedResult.TotalLines}");
        output.AppendLine($"  Total Words: {mergedResult.Lines.Count}");
        output.AppendLine();

        #endregion

        #region Add Column Headers

        output.AppendLine("-".PadRight(100, '-'));
        output.AppendLine($"{"Line",-4} | {"Status",-6} | {"Content",-75}");
        output.AppendLine("-".PadRight(100, '-'));

        #endregion

        #region Process Line Groups

        foreach (var lineGroup in mergedResult.LineGroups.OrderBy(x => x.Key))
        {
            var lineNumber = lineGroup.Key;
            var words = lineGroup.Value;

            #region Output Each Word in Line

            foreach (var word in words)
            {
                #region Format Status

                string statusSymbol = word.Operation switch
                {
                    DiffOperation.Equal => "  ✓",
                    DiffOperation.Insert => "  +",
                    DiffOperation.Delete => "  -",
                    DiffOperation.Modified => "  ~",
                    _ => "  ?"
                };

                #endregion

                #region Output Line

                output.AppendLine($"{lineNumber,-4} | {statusSymbol,-6} | {word.Content,-75}");

                #endregion
            }

            #endregion
        }

        #endregion

        #region Add Footer

        output.AppendLine("-".PadRight(100, '-'));
        output.AppendLine("=".PadRight(100, '='));

        #endregion

        return output.ToString();
    }

    public void WriteMergedDiffToFile(MergedDiffResult mergedResult, string oldFileName, string newFileName, string outputPath)
    {
        #region Format Merged Diff

        var formattedDiff = FormatMergedDiffCompact(mergedResult, oldFileName, newFileName);

        #endregion

        #region Write to File

        File.WriteAllText(outputPath, formattedDiff);

        #endregion
    }

    #endregion
}
