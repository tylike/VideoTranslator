using System.Text;
using VideoTranslator.Interfaces;
using VideoTranslator.SRT.Core.Interfaces;
using VideoTranslator.SRT.Core.Models;
using VT.Core;

namespace VideoTranslator.Services;

public class SubtitleService : ISubtitleService
{
    #region 私有字段

    private readonly ISrtReader _srtReader;
    private readonly ISrtWriter _srtWriter;

    #endregion

    #region 构造函数

    public SubtitleService(ISrtReader srtReader, ISrtWriter srtWriter)
    {
        _srtReader = srtReader;
        _srtWriter = srtWriter;
    }

    #endregion

    #region 读取 SRT 文件

    public async Task<List<ISrtSubtitle>> ParseSrtAsync(string srtPath)
    {
        var srtFile = await _srtReader.ReadAsync(srtPath);
        return srtFile.Subtitles;
    }

    #endregion

    #region 保存 SRT 文件

    public async Task SaveSrtAsync(List<ISrtSubtitle> subtitles, string outputPath)
    {
        var srtFile = new SrtFile(subtitles);
        await _srtWriter.WriteAsync(outputPath, srtFile, Encoding.UTF8);
    }

    #endregion
}
