using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using TextFileFixer.Models;

namespace TextFileFixer.Core;

public class DiffService
{
    #region Private Fields

    private readonly IDiffer _differ;
    private readonly TextPreprocessor _preprocessor;

    #endregion

    #region Constructor

    public DiffService()
    {
        _differ = new Differ();
        _preprocessor = new TextPreprocessor();
    }

    #endregion

    #region Public Methods

    public DiffResult ComputeDiff(string[] oldLines, string[] newLines)
    {
        #region Validation

        if (oldLines == null)
            throw new ArgumentNullException(nameof(oldLines));
        if (newLines == null)
            throw new ArgumentNullException(nameof(newLines));

        #endregion

        #region Build Diff Model

        var oldText = string.Join("\n", oldLines);
        var newText = string.Join("\n", newLines);

        var diffBuilder = new InlineDiffBuilder(_differ);
        var diffResult = diffBuilder.BuildDiffModel(oldText, newText);

        #endregion

        #region Convert to Custom DiffResult

        return ConvertToDiffResult(diffResult, oldLines, newLines);

        #endregion
    }

    #endregion

    #region Word-Level Diff

    public WordDiffResult ComputeWordDiff(string[] oldLines, string[] newLines)
    {
        #region Validation

        if (oldLines == null)
            throw new ArgumentNullException(nameof(oldLines));
        if (newLines == null)
            throw new ArgumentNullException(nameof(newLines));

        #endregion

        #region Preprocess Text

        var oldPreprocessed = _preprocessor.Preprocess(oldLines);
        var newPreprocessed = _preprocessor.Preprocess(newLines);

        #endregion

        #region Extract Processed Words

        var oldProcessedWords = oldPreprocessed.WordLines.Select(w => w.ProcessedText).ToArray();
        var newProcessedWords = newPreprocessed.WordLines.Select(w => w.ProcessedText).ToArray();

        #endregion

        #region Build Diff Model

        var oldText = string.Join("\n", oldProcessedWords);
        var newText = string.Join("\n", newProcessedWords);

        var diffBuilder = new InlineDiffBuilder(_differ);
        var diffResult = diffBuilder.BuildDiffModel(oldText, newText);

        #endregion

        #region Convert to WordDiffResult

        return ConvertToWordDiffResult(diffResult, oldPreprocessed, newPreprocessed);

        #endregion
    }

    public (WordDiffResult DiffResult, PreprocessedText OldPreprocessed, PreprocessedText NewPreprocessed) ComputeWordDiffWithPreprocessed(string[] oldLines, string[] newLines)
    {
        #region Validation

        if (oldLines == null)
            throw new ArgumentNullException(nameof(oldLines));
        if (newLines == null)
            throw new ArgumentNullException(nameof(newLines));

        #endregion

        #region Preprocess Text

        var oldPreprocessed = _preprocessor.Preprocess(oldLines);
        var newPreprocessed = _preprocessor.Preprocess(newLines);

        #endregion

        #region Extract Processed Words

        var oldProcessedWords = oldPreprocessed.WordLines.Select(w => w.ProcessedText).ToArray();
        var newProcessedWords = newPreprocessed.WordLines.Select(w => w.ProcessedText).ToArray();

        #endregion

        #region Build Diff Model

        var oldText = string.Join("\n", oldProcessedWords);
        var newText = string.Join("\n", newProcessedWords);

        var diffBuilder = new InlineDiffBuilder(_differ);
        var diffResult = diffBuilder.BuildDiffModel(oldText, newText);

        #endregion

        #region Convert to WordDiffResult

        var wordDiffResult = ConvertToWordDiffResult(diffResult, oldPreprocessed, newPreprocessed);

        #endregion

        return (wordDiffResult, oldPreprocessed, newPreprocessed);
    }

    #endregion

    #region Private Methods

    #region Conversion

    private DiffResult ConvertToDiffResult(DiffPaneModel diffModel, string[] oldLines, string[] newLines)
    {
        #region Initialize Result

        var result = new DiffResult();
        int oldIndex = 0;
        int newIndex = 0;

        #endregion

        #region Process Diff Lines

        foreach (var line in diffModel.Lines)
        {
            #region Handle Unchanged Lines

            if (line.Type == ChangeType.Unchanged)
            {
                result.Items.Add(new DiffResultItem(
                    DiffOperation.Equal,
                    line.Text,
                    oldIndex + 1,
                    newIndex + 1
                ));
                result.EqualCount++;
                oldIndex++;
                newIndex++;
            }

            #endregion

            #region Handle Inserted Lines

            else if (line.Type == ChangeType.Inserted)
            {
                result.Items.Add(new DiffResultItem(
                    DiffOperation.Insert,
                    line.Text,
                    -1,
                    newIndex + 1
                ));
                result.InsertCount++;
                newIndex++;
            }

            #endregion

            #region Handle Deleted Lines

            else if (line.Type == ChangeType.Deleted)
            {
                result.Items.Add(new DiffResultItem(
                    DiffOperation.Delete,
                    line.Text,
                    oldIndex + 1,
                    -1
                ));
                result.DeleteCount++;
                oldIndex++;
            }

            #endregion

            #region Handle Modified Lines

            else if (line.Type == ChangeType.Modified)
            {
                result.Items.Add(new DiffResultItem(
                    DiffOperation.Modified,
                    line.Text,
                    oldIndex + 1,
                    newIndex + 1
                ));
                result.ModifiedCount++;
                oldIndex++;
                newIndex++;
            }

            #endregion

            #region Handle Imaginary Lines

            else if (line.Type == ChangeType.Imaginary)
            {
                continue;
            }

            #endregion
        }

        #endregion

        return result;
    }

    #endregion

    #region Word-Level Conversion

    private WordDiffResult ConvertToWordDiffResult(DiffPaneModel diffModel, PreprocessedText oldPreprocessed, PreprocessedText newPreprocessed)
    {
        #region Initialize Result

        var result = new WordDiffResult();
        int oldIndex = 0;
        int newIndex = 0;

        #endregion

        #region Process Diff Lines

        foreach (var line in diffModel.Lines)
        {
            #region Handle Unchanged Lines

            if (line.Type == ChangeType.Unchanged)
            {
                if (oldIndex < oldPreprocessed.WordLines.Count)
                {
                    var wordLine = oldPreprocessed.WordLines[oldIndex];
                    result.Items.Add(new WordDiffResultItem(DiffOperation.Equal, wordLine));
                    result.EqualCount++;
                }
                oldIndex++;
                newIndex++;
            }

            #endregion

            #region Handle Inserted Lines

            else if (line.Type == ChangeType.Inserted)
            {
                if (newIndex < newPreprocessed.WordLines.Count)
                {
                    var wordLine = newPreprocessed.WordLines[newIndex];
                    result.Items.Add(new WordDiffResultItem(DiffOperation.Insert, wordLine));
                    result.InsertCount++;
                }
                newIndex++;
            }

            #endregion

            #region Handle Deleted Lines

            else if (line.Type == ChangeType.Deleted)
            {
                if (oldIndex < oldPreprocessed.WordLines.Count)
                {
                    var wordLine = oldPreprocessed.WordLines[oldIndex];
                    result.Items.Add(new WordDiffResultItem(DiffOperation.Delete, wordLine));
                    result.DeleteCount++;
                }
                oldIndex++;
            }

            #endregion

            #region Handle Modified Lines

            else if (line.Type == ChangeType.Modified)
            {
                if (oldIndex < oldPreprocessed.WordLines.Count)
                {
                    var wordLine = oldPreprocessed.WordLines[oldIndex];
                    result.Items.Add(new WordDiffResultItem(DiffOperation.Modified, wordLine));
                    result.ModifiedCount++;
                }
                oldIndex++;
                newIndex++;
            }

            #endregion

            #region Handle Imaginary Lines

            else if (line.Type == ChangeType.Imaginary)
            {
                continue;
            }

            #endregion
        }

        #endregion

        return result;
    }

    #endregion

    #endregion
}
