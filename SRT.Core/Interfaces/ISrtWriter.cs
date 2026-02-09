using System.Text;
using System.Threading.Tasks;
using VideoTranslator.SRT.Core.Models;

namespace VideoTranslator.SRT.Core.Interfaces;

public interface ISrtWriter
{
    #region 同步方法

    void Write(string filePath, SrtFile srtFile);

    void Write(string filePath, SrtFile srtFile, Encoding encoding);

    string WriteToString(SrtFile srtFile);

    #endregion

    #region 异步方法

    Task WriteAsync(string filePath, SrtFile srtFile);

    Task WriteAsync(string filePath, SrtFile srtFile, Encoding encoding);

    #endregion

    #region 批量操作

    void WriteMultiple(string filePath, SrtFile[] srtFiles);

    Task WriteMultipleAsync(string filePath, SrtFile[] srtFiles);

    #endregion
}
