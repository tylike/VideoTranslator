using System.Net.Http;
using System.Text;
using System.Text.Json;
using VideoTranslator.Interfaces;
using VideoTranslator.SRT.Core.Models;

namespace VadTimeProcessor.Services;

/// <summary>
/// Whisper服务器客户端 - 通过HTTP API调用whisper-server
/// </summary>
public static class WhisperServerClient
{
    #region 常量

    private const string DefaultHost = "127.0.0.1";
    private const int DefaultPort = 8080;
    private const string InferencePath = "/inference";
    private const string LoadPath = "/load";

    #endregion

    #region 私有字段

    private static HttpClient? _httpClient;
    private static string _serverUrl = $"http://{DefaultHost}:{DefaultPort}";
    private static IProgressService? _progressService;

    #endregion

    #region 公共属性

    /// <summary>
    /// 获取或设置服务器URL
    /// </summary>
    public static string ServerUrl
    {
        get => _serverUrl;
        set => _serverUrl = value;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置进度服务实例
    /// </summary>
    /// <param name="progressService">进度服务实例</param>
    public static void SetProgressService(IProgressService progressService)
    {
        _progressService = progressService;
    }

    /// <summary>
    /// 获取或创建HTTP客户端实例
    /// </summary>
    private static HttpClient GetHttpClient()
    {
        if (_httpClient == null)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(30);
        }
        return _httpClient;
    }

    /// <summary>
    /// 设置服务器地址
    /// </summary>
    /// <param name="host">主机地址</param>
    /// <param name="port">端口</param>
    public static void SetServerUrl(string host = DefaultHost, int port = DefaultPort)
    {
        _serverUrl = $"http://{host}:{port}";
    }

    /// <summary>
    /// 设置完整的服务器URL
    /// </summary>
    /// <param name="url">完整的URL（如 http://127.0.0.1:8080）</param>
    public static void SetServerUrl(string url)
    {
        _serverUrl = url;
    }

    /// <summary>
    /// 加载模型
    /// </summary>
    /// <param name="modelPath">模型文件路径</param>
    public static async Task LoadModelAsync(string modelPath)
    {
        #region 构建请求

        var content = new MultipartFormDataContent();
        content.Add(new StringContent(modelPath), "model");

        #endregion

        #region 发送请求

        _progressService?.Title("加载Whisper模型");
        _progressService?.Report($"模型路径: {modelPath}");
        _progressService?.Report($"服务器: {_serverUrl}");
        _progressService?.Report();

        var response = await GetHttpClient().PostAsync($"{_serverUrl}{LoadPath}", content);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();
        _progressService?.Success($"模型加载成功: {responseText}");
        _progressService?.Report();

        #endregion
    }

    /// <summary>
    /// 使用Whisper服务器生成SRT字幕（使用verbose_json获取详细信息）
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="outputPath">输出文件路径（不含扩展名）</param>
    /// <param name="temperature">温度参数（默认0.0）</param>
    /// <returns>生成的SRT文件路径</returns>
    public static async Task<string> GenerateSrtWithWhisperServerAsync(
        string audioPath,
        string outputPath,
        double temperature = 0.0)
    {
        #region 验证参数

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件未找到: {audioPath}");
        }

        #endregion

        #region 获取音频信息

        var fileInfo = new FileInfo(audioPath);
        double fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
        _progressService?.Report($"音频文件大小: {fileSizeMB:F2} MB");

        #endregion

        #region 构建请求

        var content = new MultipartFormDataContent();
        
        #region 添加音频文件

        var fileBytes = await File.ReadAllBytesAsync(audioPath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "file", Path.GetFileName(audioPath));

        #endregion

        #region 添加参数

        content.Add(new StringContent(temperature.ToString("F2")), "temperature");
        content.Add(new StringContent("verbose_json"), "response_format");

        #endregion

        #endregion

        #region 执行推理

        _progressService?.Title("使用Whisper服务器生成SRT字幕");
        _progressService?.Report($"音频: {audioPath}");
        _progressService?.Report($"服务器: {_serverUrl}");
        _progressService?.Report($"温度: {temperature:F2}");
        _progressService?.Report($"输出: {outputPath}.srt");
        _progressService?.Report();
        _progressService?.Report("正在发送请求到服务器...");
        _progressService?.Report("（服务器模式：模型已加载，处理速度较快）");
        _progressService?.Report("（注意：长音频可能需要较长时间，请耐心等待...）");
        _progressService?.Report();

        var startTime = DateTime.Now;
        var response = await GetHttpClient().PostAsync($"{_serverUrl}{InferencePath}", content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var elapsed = (DateTime.Now - startTime).TotalSeconds;

        _progressService?.Success($"处理完成！耗时: {elapsed:F2}秒");
        _progressService?.Report();

        #endregion

        #region 解析verbose_json并生成SRT

        var srtFile = ConvertVerboseJsonToSrt(jsonResponse);

        #endregion

        #region 保存SRT文件

        var srtPath = $"{outputPath}.srt";
        await srtFile.WriteAsync(srtPath);

        _progressService?.Success($"SRT字幕生成成功: {srtPath}");
        _progressService?.Report();

        #endregion

        return srtPath;
    }

    /// <summary>
    /// 将verbose_json转换为SRT格式
    /// </summary>
    /// <param name="verboseJson">verbose_json字符串</param>
    /// <returns>SRT文件对象</returns>
    private static SrtFile ConvertVerboseJsonToSrt(string verboseJson)
    {
        #region 解析JSON

        using var jsonDoc = System.Text.Json.JsonDocument.Parse(verboseJson);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("segments", out var segmentsElement))
        {
            throw new InvalidOperationException("verbose_json中未找到segments字段");
        }

        #endregion

        #region 生成SRT内容

        var srtFile = new SrtFile();
        int index = 1;

        foreach (var segment in segmentsElement.EnumerateArray())
        {
            if (!segment.TryGetProperty("start", out var startElement) ||
                !segment.TryGetProperty("end", out var endElement) ||
                !segment.TryGetProperty("text", out var textElement))
            {
                continue;
            }

            double startSeconds = startElement.GetDouble();
            double endSeconds = endElement.GetDouble();
            string text = textElement.GetString() ?? string.Empty;

            #region 创建字幕

            var subtitle = new SrtSubtitle(
                index,
                TimeSpan.FromSeconds(startSeconds),
                TimeSpan.FromSeconds(endSeconds),
                text
            );

            srtFile.AddSubtitle(subtitle);

            #endregion

            index++;
        }

        return srtFile;

        #endregion
    }

    /// <summary>
    /// 使用Whisper服务器生成文本
    /// </summary>
    /// <param name="audioPath">音频文件路径</param>
    /// <param name="temperature">温度参数（默认0.0）</param>
    /// <returns>识别的文本</returns>
    public static async Task<string> GenerateTextWithWhisperServerAsync(
        string audioPath,
        double temperature = 0.0)
    {
        #region 验证参数

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException($"音频文件未找到: {audioPath}");
        }

        #endregion

        #region 构建请求

        var content = new MultipartFormDataContent();
        
        #region 添加音频文件

        var fileBytes = await File.ReadAllBytesAsync(audioPath);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "file", Path.GetFileName(audioPath));

        #endregion

        #region 添加参数

        content.Add(new StringContent(temperature.ToString("F2")), "temperature");
        content.Add(new StringContent("text"), "response_format");

        #endregion

        #endregion

        #region 执行推理

        var response = await GetHttpClient().PostAsync($"{_serverUrl}{InferencePath}", content);
        response.EnsureSuccessStatusCode();

        var textContent = await response.Content.ReadAsStringAsync();

        #endregion

        return textContent;
    }

    #endregion
}
