using TextFileFixer.Models;

namespace TextFileFixer.Services;

public class DiffOutputFormatter
{
    #region Public Methods

    public string FormatDiffResult(DiffResult diffResult, string oldFileName, string newFileName)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Add Header

        output.AppendLine("=".PadRight(80, '='));
        output.AppendLine($"FILE DIFF: {oldFileName} -> {newFileName}");
        output.AppendLine("=".PadRight(80, '='));

        #endregion

        #region Add Summary

        output.AppendLine();
        output.AppendLine("Summary:");
        output.AppendLine($"  Lines Added:    {diffResult.InsertCount}");
        output.AppendLine($"  Lines Removed:  {diffResult.DeleteCount}");
        output.AppendLine($"  Unchanged:      {diffResult.EqualCount}");
        output.AppendLine($"  Total Changes:  {diffResult.InsertCount + diffResult.DeleteCount}");
        output.AppendLine();

        #endregion

        #region Add Diff Details

        output.AppendLine("-".PadRight(80, '-'));
        output.AppendLine("Details:");
        output.AppendLine("-".PadRight(80, '-'));
        output.AppendLine();

        foreach (var item in diffResult.Items)
        {
            #region Format Based on Operation

            switch (item.Operation)
            {
                case DiffOperation.Equal:
                    output.AppendLine($"  {item.Content}");
                    break;

                case DiffOperation.Insert:
                    output.AppendLine($"+ {item.Content}");
                    break;

                case DiffOperation.Delete:
                    output.AppendLine($"- {item.Content}");
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

    public string FormatDiffResultCompact(DiffResult diffResult)
    {
        #region Initialize Output

        var output = new System.Text.StringBuilder();

        #endregion

        #region Process Items

        foreach (var item in diffResult.Items)
        {
            #region Format Based on Operation

            switch (item.Operation)
            {
                case DiffOperation.Equal:
                    output.AppendLine($"  {item.Content}");
                    break;

                case DiffOperation.Insert:
                    output.AppendLine($"+ {item.Content}");
                    break;

                case DiffOperation.Delete:
                    output.AppendLine($"- {item.Content}");
                    break;
            }

            #endregion
        }

        #endregion

        return output.ToString();
    }

    public void WriteDiffToFile(DiffResult diffResult, string oldFileName, string newFileName, string outputPath)
    {
        #region Format Diff

        var formattedDiff = FormatDiffResult(diffResult, oldFileName, newFileName);

        #endregion

        #region Write to File

        File.WriteAllText(outputPath, formattedDiff);

        #endregion
    }

    #endregion
}
