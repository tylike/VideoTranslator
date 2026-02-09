using TextFileFixer.Services;

namespace TextFileFixer;

class Program
{
    #region Main Method

    static async Task Main(string[] args)
    {
        #region Display Welcome Message

        Console.WriteLine("Text File Diff Tool - Word-Level Comparison with Merge");
        Console.WriteLine("=====================================================");
        Console.WriteLine();

        #endregion

        #region Get File Paths

        string oldFilePath = @"d:\VideoTranslator\videoProjects\25\格式对.txt";
        string newFilePath = @"d:\VideoTranslator\videoProjects\25\内容对.txt";

        Console.WriteLine($"Comparing files:");
        Console.WriteLine($"  Old: {oldFilePath}");
        Console.WriteLine($"  New: {newFilePath}");
        Console.WriteLine();

        #endregion

        #region Validate Files

        if (!File.Exists(oldFilePath))
        {
            Console.WriteLine($"Error: Old file not found: {oldFilePath}");
            return;
        }

        if (!File.Exists(newFilePath))
        {
            Console.WriteLine($"Error: New file not found: {newFilePath}");
            return;
        }

        #endregion

        #region Initialize Services

        var fileComparer = new FileComparerService();
        var mergedFormatter = new MergedDiffFormatter();

        #endregion

        #region Compare and Merge Files

        Console.WriteLine("Comparing and merging files...");
        Console.WriteLine();

        try
        {
            #region Perform Comparison and Merge

            var mergedResult = await fileComparer.CompareAndMergeFilesAsync(oldFilePath, newFilePath);

            #endregion

            #region Display Results

            var oldFileName = Path.GetFileName(oldFilePath);
            var newFileName = Path.GetFileName(newFilePath);
            var formattedMergedDiff = mergedFormatter.FormatMergedDiffCompact(mergedResult, oldFileName, newFileName);

            Console.WriteLine(formattedMergedDiff);

            #endregion

            #region Save to File

            string outputPath = Path.Combine(
                Path.GetDirectoryName(oldFilePath) ?? ".",
                $"merged_{Path.GetFileNameWithoutExtension(oldFileName)}_{Path.GetFileNameWithoutExtension(newFileName)}.txt"
            );

            mergedFormatter.WriteMergedDiffToFile(mergedResult, oldFileName, newFileName, outputPath);
            Console.WriteLine($"Merged diff result saved to: {outputPath}");

            #endregion
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during comparison: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        #endregion
    }

    #endregion
}
