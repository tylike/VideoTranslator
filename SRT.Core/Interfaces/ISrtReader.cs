using System.Threading.Tasks;
using VideoTranslator.SRT.Core.Models;

namespace VideoTranslator.SRT.Core.Interfaces;

public interface ISrtReader
{
    #region 同步方法

    SrtFile Read(string filePath);

    SrtFile ReadFromString(string content);

    #endregion

    #region 异步方法

    Task<SrtFile> ReadAsync(string filePath);

    Task<SrtFile> ReadFromStringAsync(string content);

    #endregion

    #region 验证方法

    bool IsValidSrtFile(string filePath);

    bool IsValidSrtContent(string content);

    #endregion
}
