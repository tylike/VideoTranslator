using TextFileFixer.Models;

namespace TextFileFixer.Core;

public class DiffMerger
{
    #region Public Methods

    public MergedDiffResult MergeDiff(WordDiffResult diffResult, PreprocessedText oldPreprocessed, PreprocessedText newPreprocessed)
    {
        #region Initialize Result

        var result = new MergedDiffResult();
        var lineGroups = new Dictionary<int, List<MergedWordLine>>();

        #endregion

        #region Process Diff Items

        foreach (var item in diffResult.Items)
        {
            #region Handle Equal Items

            if (item.Operation == DiffOperation.Equal)
            {
                var mergedLine = new MergedWordLine(
                    item.OriginalText,
                    item.OriginalLineNumber,
                    DiffOperation.Equal,
                    isFromNewFile: true
                );

                result.Lines.Add(mergedLine);
                AddToLineGroup(lineGroups, mergedLine);
            }

            #endregion

            #region Handle Insert Items (from new file)

            else if (item.Operation == DiffOperation.Insert)
            {
                var mergedLine = new MergedWordLine(
                    item.OriginalText,
                    item.OriginalLineNumber,
                    DiffOperation.Insert,
                    isFromNewFile: true
                );

                result.Lines.Add(mergedLine);
                AddToLineGroup(lineGroups, mergedLine);
            }

            #endregion

            #region Handle Delete Items (from old file only - skip in merged result)

            else if (item.Operation == DiffOperation.Delete)
            {
                var mergedLine = new MergedWordLine(
                    item.OriginalText,
                    item.OriginalLineNumber,
                    DiffOperation.Delete,
                    isFromNewFile: false
                );

                result.Lines.Add(mergedLine);
                AddToLineGroup(lineGroups, mergedLine);
            }

            #endregion

            #region Handle Modified Items

            else if (item.Operation == DiffOperation.Modified)
            {
                var mergedLine = new MergedWordLine(
                    item.OriginalText,
                    item.OriginalLineNumber,
                    DiffOperation.Modified,
                    isFromNewFile: true
                );

                result.Lines.Add(mergedLine);
                AddToLineGroup(lineGroups, mergedLine);
            }

            #endregion
        }

        #endregion

        #region Set Line Groups

        result.LineGroups = lineGroups;
        result.TotalLines = lineGroups.Count;

        #endregion

        return result;
    }

    #endregion

    #region Private Methods

    #region Line Grouping

    private void AddToLineGroup(Dictionary<int, List<MergedWordLine>> lineGroups, MergedWordLine line)
    {
        #region Get or Create Group

        if (!lineGroups.ContainsKey(line.OldLineNumber))
        {
            lineGroups[line.OldLineNumber] = new List<MergedWordLine>();
        }

        #endregion

        #region Add to Group

        lineGroups[line.OldLineNumber].Add(line);

        #endregion
    }

    #endregion

    #endregion
}
