namespace VideoTranslator.Interfaces;
public interface IProgressService
{
    void SetStatusMessage(string message, MessageType type = MessageType.Info, bool newline = true, bool log = true);

    #region 进度条
    void ShowProgress(bool marquee = false);
    void HideProgress();
    void ResetProgress();
    void ReportProgress(double value);
    void SetProgressMaxValue(double value);
    #endregion

    void ReportProgress(double value, string message = null, double maximum = 100)
    {
        if (maximum != 100)
        {
            SetProgressMaxValue(maximum);
        }
        ReportProgress(value);
        if (message != null)
        {
            SetStatusMessage(message);
        }
    }

    void Report() => Report("", MessageType.Info);

    #region 分类
    void Report(string message, MessageType messageType = MessageType.Info) => SetStatusMessage(message, messageType);
    void Title(string message) => SetStatusMessage(message, MessageType.Title);
    void Success(string message) => SetStatusMessage(message, MessageType.Success);
    void Error(string message) => SetStatusMessage(message, MessageType.Error);
    void Warning(string message) => SetStatusMessage(message, MessageType.Warning);
    void Debug(string message) => SetStatusMessage(message, MessageType.Debug); 
    #endregion

}
