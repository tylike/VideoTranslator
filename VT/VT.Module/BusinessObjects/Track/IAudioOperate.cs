namespace VT.Module.BusinessObjects;

public interface  IAudioOperate
{
    //行为:人声、背景 分离
    Task Separate();
    /// <summary>
    /// 识别字幕
    /// </summary>
    /// <returns></returns>
    Task Recognition();
    /// <summary>
    /// 使用vad段落识别字幕
    /// </summary>
    /// <returns></returns>
    Task RecognitionByVad();
}

