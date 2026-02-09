using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VideoTranslator.SRT.Core.Interfaces;
using VideoTranslator.SRT.Core.Models;

namespace VideoTranslator.SRT.Core.Services;

public class SrtWriter : ISrtWriter
{
    #region 私有字段

    private const string DefaultEncoding = "UTF-8";

    #endregion

    #region 同步方法

    public void Write(string filePath, SrtFile srtFile)
    {
        Write(filePath, srtFile, Encoding.UTF8);
    }

    public void Write(string filePath, SrtFile srtFile, Encoding encoding)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        if (encoding == null)
        {
            encoding = Encoding.UTF8;
        }

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string content = WriteToString(srtFile);
        File.WriteAllText(filePath, content, encoding);
    }

    public string WriteToString(SrtFile srtFile)
    {
        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        var builder = new StringBuilder();
        int displayIndex = 1;

        foreach (var subtitle in srtFile.Subtitles)
        {
            builder.AppendLine(displayIndex.ToString());
            builder.AppendLine(subtitle.ToSrtTimeString());
            builder.AppendLine(subtitle.Text);
            builder.AppendLine();
            displayIndex++;
        }

        return builder.ToString().TrimEnd();
    }

    #endregion

    #region 异步方法

    public async Task WriteAsync(string filePath, SrtFile srtFile)
    {
        await WriteAsync(filePath, srtFile, Encoding.UTF8);
    }

    public async Task WriteAsync(string filePath, SrtFile srtFile, Encoding encoding)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (srtFile == null)
        {
            throw new ArgumentNullException(nameof(srtFile));
        }

        if (encoding == null)
        {
            encoding = Encoding.UTF8;
        }

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string content = WriteToString(srtFile);
        await File.WriteAllTextAsync(filePath, content, encoding);
    }

    #endregion

    #region 批量操作

    public void WriteMultiple(string filePath, SrtFile[] srtFiles)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (srtFiles == null || srtFiles.Length == 0)
        {
            throw new ArgumentNullException(nameof(srtFiles));
        }

        var mergedFile = new SrtFile();
        int currentIndex = 1;

        foreach (var srtFile in srtFiles)
        {
            foreach (var subtitle in srtFile.Subtitles)
            {
                subtitle.Index = currentIndex;
                mergedFile.AddSubtitle(subtitle);
                currentIndex++;
            }
        }

        Write(filePath, mergedFile);
    }

    public async Task WriteMultipleAsync(string filePath, SrtFile[] srtFiles)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (srtFiles == null || srtFiles.Length == 0)
        {
            throw new ArgumentNullException(nameof(srtFiles));
        }

        var mergedFile = new SrtFile();
        int currentIndex = 1;

        foreach (var srtFile in srtFiles)
        {
            foreach (var subtitle in srtFile.Subtitles)
            {
                subtitle.Index = currentIndex;
                mergedFile.AddSubtitle(subtitle);
                currentIndex++;
            }
        }

        await WriteAsync(filePath, mergedFile);
    }

    #endregion
}
