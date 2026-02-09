using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoTranslator.SRT.Core.Services;
using VT.Core;

namespace VideoTranslator.SRT.Core.Models;

public class SrtFile
{
    #region 属性

    public List<ISrtSubtitle> Subtitles { get; set; } = new();

    #endregion

    #region 计算属性

    public int Count => Subtitles.Count;

    public TimeSpan TotalDuration => Subtitles.Any() ? Subtitles.Max(s => s.EndTime) : TimeSpan.Zero;

    #endregion

    #region 构造函数

    public SrtFile()
    {
    }

    public SrtFile(IEnumerable<ISrtSubtitle> subtitles)
    {
        Subtitles = new List<ISrtSubtitle>(subtitles);
    }

    #endregion

    #region 静态读取方法

    public static SrtFile Read(string filePath)
    {
        var reader = new SrtReader();
        return reader.Read(filePath);
    }

    public static async Task<SrtFile> ReadAsync(string filePath)
    {
        var reader = new SrtReader();
        return await reader.ReadAsync(filePath);
    }

    public static SrtFile FromString(string content)
    {
        var reader = new SrtReader();
        return reader.ReadFromString(content);
    }

    public static async Task<SrtFile> FromStringAsync(string content)
    {
        var reader = new SrtReader();
        return await reader.ReadFromStringAsync(content);
    }

    #endregion

    #region 静态写入方法

    public void Write(string filePath)
    {
        var writer = new SrtWriter();
        writer.Write(filePath, this);
    }

    public void Write(string filePath, Encoding encoding)
    {
        var writer = new SrtWriter();
        writer.Write(filePath, this, encoding);
    }

    public async Task WriteAsync(string filePath)
    {
        var writer = new SrtWriter();
        await writer.WriteAsync(filePath, this);
    }

    public async Task WriteAsync(string filePath, Encoding encoding)
    {
        var writer = new SrtWriter();
        await writer.WriteAsync(filePath, this, encoding);
    }

    public override string ToString()
    {
        var writer = new SrtWriter();
        return writer.WriteToString(this);
    }

    #endregion

    #region 公共方法

    public void AddSubtitle(ISrtSubtitle subtitle)
    {
        Subtitles.Add(subtitle);
        ReindexSubtitles();
    }

    public void RemoveSubtitle(int index)
    {
        var subtitle = Subtitles.FirstOrDefault(s => s.Index == index);
        if (subtitle != null)
        {
            Subtitles.Remove(subtitle);
            ReindexSubtitles();
        }
    }

    public ISrtSubtitle? GetSubtitle(int index)
    {
        return Subtitles.FirstOrDefault(s => s.Index == index);
    }

    public void ReindexSubtitles()
    {
        for (int i = 0; i < Subtitles.Count; i++)
        {
            Subtitles[i].Index = i + 1;
        }
    }

    #endregion
}
