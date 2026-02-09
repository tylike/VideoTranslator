using TextFileFixer.Core;
using TextFileFixer.Models;

namespace TextFileFixer.Services;

public class FileComparerService
{
    #region Private Fields

    private readonly DiffService _diffService;
    private readonly DiffMerger _diffMerger;

    #endregion

    #region Constructor

    public FileComparerService()
    {
        _diffService = new DiffService();
        _diffMerger = new DiffMerger();
    }

    #endregion

    #region Public Methods

    public DiffResult CompareFiles(string oldFilePath, string newFilePath)
    {
        #region Validation

        if (string.IsNullOrEmpty(oldFilePath))
            throw new ArgumentException("Old file path cannot be null or empty", nameof(oldFilePath));
        if (string.IsNullOrEmpty(newFilePath))
            throw new ArgumentException("New file path cannot be null or empty", nameof(newFilePath));

        #endregion

        #region Read Files

        var oldLines = ReadFileLines(oldFilePath);
        var newLines = ReadFileLines(newFilePath);

        #endregion

        #region Compute Diff

        var result = _diffService.ComputeDiff(oldLines, newLines);

        #endregion

        return result;
    }

    public async Task<DiffResult> CompareFilesAsync(string oldFilePath, string newFilePath)
    {
        #region Validation

        if (string.IsNullOrEmpty(oldFilePath))
            throw new ArgumentException("Old file path cannot be null or empty", nameof(oldFilePath));
        if (string.IsNullOrEmpty(newFilePath))
            throw new ArgumentException("New file path cannot be null or empty", nameof(newFilePath));

        #endregion

        #region Read Files Asynchronously

        var oldLines = await ReadFileLinesAsync(oldFilePath);
        var newLines = await ReadFileLinesAsync(newFilePath);

        #endregion

        #region Compute Diff

        var result = _diffService.ComputeDiff(oldLines, newLines);

        #endregion

        return result;
    }

    public WordDiffResult CompareFilesByWord(string oldFilePath, string newFilePath)
    {
        #region Validation

        if (string.IsNullOrEmpty(oldFilePath))
            throw new ArgumentException("Old file path cannot be null or empty", nameof(oldFilePath));
        if (string.IsNullOrEmpty(newFilePath))
            throw new ArgumentException("New file path cannot be null or empty", nameof(newFilePath));

        #endregion

        #region Read Files

        var oldLines = ReadFileLines(oldFilePath);
        var newLines = ReadFileLines(newFilePath);

        #endregion

        #region Compute Word Diff

        var result = _diffService.ComputeWordDiff(oldLines, newLines);

        #endregion

        return result;
    }

    public async Task<WordDiffResult> CompareFilesByWordAsync(string oldFilePath, string newFilePath)
    {
        #region Validation

        if (string.IsNullOrEmpty(oldFilePath))
            throw new ArgumentException("Old file path cannot be null or empty", nameof(oldFilePath));
        if (string.IsNullOrEmpty(newFilePath))
            throw new ArgumentException("New file path cannot be null or empty", nameof(newFilePath));

        #endregion

        #region Read Files Asynchronously

        var oldLines = await ReadFileLinesAsync(oldFilePath);
        var newLines = await ReadFileLinesAsync(newFilePath);

        #endregion

        #region Compute Word Diff

        var result = _diffService.ComputeWordDiff(oldLines, newLines);

        #endregion

        return result;
    }

    public MergedDiffResult CompareAndMergeFiles(string oldFilePath, string newFilePath)
    {
        #region Validation

        if (string.IsNullOrEmpty(oldFilePath))
            throw new ArgumentException("Old file path cannot be null or empty", nameof(oldFilePath));
        if (string.IsNullOrEmpty(newFilePath))
            throw new ArgumentException("New file path cannot be null or empty", nameof(newFilePath));

        #endregion

        #region Read Files

        var oldLines = ReadFileLines(oldFilePath);
        var newLines = ReadFileLines(newFilePath);

        #endregion

        #region Compute Word Diff with Preprocessed Data

        var (diffResult, oldPreprocessed, newPreprocessed) = _diffService.ComputeWordDiffWithPreprocessed(oldLines, newLines);

        #endregion

        #region Merge Diff

        var mergedResult = _diffMerger.MergeDiff(diffResult, oldPreprocessed, newPreprocessed);

        #endregion

        return mergedResult;
    }

    public async Task<MergedDiffResult> CompareAndMergeFilesAsync(string oldFilePath, string newFilePath)
    {
        #region Validation

        if (string.IsNullOrEmpty(oldFilePath))
            throw new ArgumentException("Old file path cannot be null or empty", nameof(oldFilePath));
        if (string.IsNullOrEmpty(newFilePath))
            throw new ArgumentException("New file path cannot be null or empty", nameof(newFilePath));

        #endregion

        #region Read Files Asynchronously

        var oldLines = await ReadFileLinesAsync(oldFilePath);
        var newLines = await ReadFileLinesAsync(newFilePath);

        #endregion

        #region Compute Word Diff with Preprocessed Data

        var (diffResult, oldPreprocessed, newPreprocessed) = _diffService.ComputeWordDiffWithPreprocessed(oldLines, newLines);

        #endregion

        #region Merge Diff

        var mergedResult = _diffMerger.MergeDiff(diffResult, oldPreprocessed, newPreprocessed);

        #endregion

        return mergedResult;
    }

    #endregion

    #region Private Methods

    #region File Reading

    private string[] ReadFileLines(string filePath)
    {
        #region Check File Existence

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        #endregion

        #region Read All Lines

        var lines = File.ReadAllLines(filePath);

        #endregion

        return lines;
    }

    private async Task<string[]> ReadFileLinesAsync(string filePath)
    {
        #region Check File Existence

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        #endregion

        #region Read All Lines Asynchronously

        var lines = await File.ReadAllLinesAsync(filePath);

        #endregion

        return lines;
    }

    #endregion

    #endregion
}
