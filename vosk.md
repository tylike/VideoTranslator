# SubtitleEdit Vosk 识别字幕和 SRT 结果逻辑分析

## 概述

SubtitleEdit 项目使用 Vosk 引擎进行语音识别，将音频转换为字幕文本，最终输出为 SRT 格式。本文档详细分析了从音频输入到 SRT 输出的完整流程。

---

## 核心文件列表

### 1. VoskAudioToText.cs
**路径**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`

**职责**: Vosk 识别的主界面和核心逻辑

### 2. VoskAudioToTextSelectedLines.cs
**路径**: `src/ui/Forms/AudioToText/VoskAudioToTextSelectedLines.cs`

**职责**: 选中行的 Vosk 识别逻辑

### 3. VoskModel.cs
**路径**: `src/libse/AudioToText/VoskModel.cs`

**职责**: Vosk 模型定义和管理

### 4. AudioToTextPostProcessor.cs
**路径**: `src/libse/AudioToText/AudioToTextPostProcessor.cs`

**职责**: 识别结果的后处理和优化

### 5. ResultText.cs
**路径**: `src/libse/AudioToText/ResultText.cs`

**职责**: 识别结果的数据结构

### 6. SubRip.cs
**路径**: `src/libse/SubtitleFormats/SubRip.cs`

**职责**: SRT 格式的读写

### 7. Paragraph.cs
**路径**: `src/libse/Common/Paragraph.cs`

**职责**: 单个字幕段落的数据结构

### 8. Subtitle.cs
**路径**: `src/libse/Common/Subtitle.cs`

**职责**: 字幕集合的数据结构

---

## 数据结构

### ResultText 类
**文件**: `src/libse/AudioToText/ResultText.cs`

```csharp
public class ResultText
{
    public string Text { get; set; }           // 识别的文本内容
    public decimal Start { get; set; }         // 开始时间（秒）
    public decimal End { get; set; }           // 结束时间（秒）
    public decimal Confidence { get; set; }     // 置信度
}
```

### Paragraph 类
**文件**: `src/libse/Common/Paragraph.cs`

```csharp
public class Paragraph
{
    public int Number { get; set; }           // 字幕序号
    public string Text { get; set; }           // 字幕文本
    public TimeCode StartTime { get; set; }    // 开始时间码
    public TimeCode EndTime { get; set; }      // 结束时间码
    public TimeCode Duration { get; }          // 持续时间
    // ... 其他属性
}
```

### Subtitle 类
**文件**: `src/libse/Common/Subtitle.cs`

```csharp
public class Subtitle
{
    public List<Paragraph> Paragraphs { get; private set; }  // 字幕段落列表
    public string Header { get; set; }                       // 文件头
    public string Footer { get; set; }                       // 文件尾
    public string FileName { get; set; }                     // 文件名
    public SubtitleFormat OriginalFormat { get; set; }       // 原始格式
    public Encoding OriginalEncoding { get; private set; }   // 原始编码
    // ... 其他属性和方法
}
```

---

## 核心流程分析

### 流程图

```
视频文件
    ↓
GenerateWavFile() - 提取音频为 WAV
    ↓
TranscribeViaVosk() - Vosk 识别
    ↓
ParseJsonToResult() - 解析 JSON 结果
    ↓
List<ResultText> - 原始识别结果
    ↓
AudioToTextPostProcessor.Fix() - 后处理
    ↓
Subtitle - 优化后的字幕对象
    ↓
SubRip.ToText() - 转换为 SRT 格式
    ↓
SRT 文件
```

---

## 详细方法分析

### 1. ButtonGenerate_Click()
**文件**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`
**行号**: 约 78-120

**功能**: 点击生成按钮时的主流程

```csharp
private void ButtonGenerate_Click(object sender, EventArgs e)
{
    // 检查模型是否存在
    if (comboBoxModels.Items.Count == 0)
    {
        buttonDownload_Click(null, null);
        return;
    }

    // 判断是否使用中心声道
    _useCenterChannelOnly = Configuration.Settings.General.FFmpegUseCenterChannelOnly &&
                            FfmpegMediaInfo.Parse(_videoFileName).HasFrontCenterAudio(_audioTrackNumber);

    if (_batchMode)
    {
        // 批量模式处理
        GenerateBatch();
        return;
    }

    // 显示进度条
    ShowProgressBar();
    var modelFileName = Path.Combine(_voskFolder, comboBoxModels.Text);
    
    // 禁用控件
    buttonGenerate.Enabled = false;
    buttonDownload.Enabled = false;
    buttonBatchMode.Enabled = false;
    comboBoxModels.Enabled = false;
    
    // 生成 WAV 文件
    var waveFileName = GenerateWavFile(_videoFileName, _audioTrackNumber);
    
    // Vosk 识别
    var transcript = TranscribeViaVosk(waveFileName, modelFileName);
    
    // 检查是否取消
    if (_cancel && (transcript == null || transcript.Count == 0 || 
        MessageBox.Show(LanguageSettings.Current.AudioToText.KeepPartialTranscription, 
        Text, MessageBoxButtons.YesNoCancel) != DialogResult.Yes))
    {
        DialogResult = DialogResult.Cancel;
        return;
    }

    // 后处理
    var postProcessor = new AudioToTextPostProcessor(GetLanguage(comboBoxModels.Text))
    {
        ParagraphMaxChars = Configuration.Settings.General.SubtitleLineMaximumLength * 2,
    };
    TranscribedSubtitle = postProcessor.Fix(AudioToTextPostProcessor.Engine.Vosk, 
        transcript, checkBoxUsePostProcessing.Checked, true, true, true, true, false);
    
    DialogResult = DialogResult.OK;
}
```

---

### 2. GenerateWavFile()
**文件**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`
**行号**: 约 335-420

**功能**: 从视频文件中提取音频并转换为 WAV 格式

```csharp
private string GenerateWavFile(string videoFileName, int audioTrackNumber)
{
    // 生成临时 WAV 文件路径
    var outWaveFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wav");
    _filesToDelete.Add(outWaveFile);
    
    // 获取 FFmpeg 进程
    var process = GetFfmpegProcess(videoFileName, audioTrackNumber, outWaveFile);
    process.Start();
    
    ShowProgressBar();
    progressBar1.Style = ProgressBarStyle.Marquee;
    double seconds = 0;
    buttonCancel.Visible = true;
    
    try
    {
        process.PriorityClass = ProcessPriorityClass.Normal;
    }
    catch
    {
        // ignored
    }

    _cancel = false;
    
    // 监控进程
    while (!process.HasExited)
    {
        Application.DoEvents();
        System.Threading.Thread.Sleep(100);
        seconds += 0.1;
        
        // 更新进度文本
        if (seconds < 60)
        {
            labelProgress.Text = string.Format(LanguageSettings.Current.AddWaveform.ExtractingSeconds, seconds);
        }
        else
        {
            labelProgress.Text = string.Format(LanguageSettings.Current.AddWaveform.ExtractingMinutes, 
                (int)(seconds / 60), (int)(seconds % 60));
        }

        Invalidate();
        
        // 检查是否取消
        if (_cancel)
        {
            process.Kill();
            progressBar1.Visible = false;
            buttonCancel.Visible = false;
            DialogResult = DialogResult.Cancel;
            return null;
        }

        // 检查磁盘空间
        if (targetDriveLetter != null && seconds > 1 && Convert.ToInt32(seconds) % 10 == 0)
        {
            try
            {
                var drive = new DriveInfo(targetDriveLetter);
                if (drive.IsReady)
                {
                    if (drive.AvailableFreeSpace < 50 * 1000000) // 50 mb
                    {
                        labelInfo.ForeColor = Color.Red;
                        labelInfo.Text = LanguageSettings.Current.AddWaveform.LowDiskSpace;
                    }
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    Application.DoEvents();
    System.Threading.Thread.Sleep(100);

    // 检查文件是否存在
    if (!File.Exists(outWaveFile))
    {
        SeLogger.Error("Generated wave file not found: " + outWaveFile + Environment.NewLine +
                       "ffmpeg: " + process.StartInfo.FileName + Environment.NewLine +
                       "Parameters: " + process.StartInfo.Arguments + Environment.NewLine +
                       "OS: " + Environment.OSVersion + Environment.NewLine +
                       "64-bit: " + Environment.Is64BitOperatingSystem);
    }

    return outWaveFile;
}
```

---

### 3. TranscribeViaVosk()
**文件**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`
**行号**: 约 230-330

**功能**: 使用 Vosk 引擎进行语音识别

```csharp
public List<ResultText> TranscribeViaVosk(string waveFileName, string modelFileName)
{
    // 设置当前目录
    Directory.SetCurrentDirectory(_voskFolder);
    Vosk.Vosk.SetLogLevel(0);
    
    // 加载模型
    if (_model == null)
    {
        labelProgress.Text = LanguageSettings.Current.AudioToText.LoadingVoskModel;
        labelProgress.Refresh();
        Application.DoEvents();
        _model = new Model(modelFileName);
    }

    // 创建识别器
    var rec = new VoskRecognizer(_model, 16000.0f);
    rec.SetMaxAlternatives(0);
    rec.SetWords(true);
    
    var list = new List<ResultText>();
    
    // 更新进度文本
    labelProgress.Text = LanguageSettings.Current.AudioToText.Transcribing;
    if (_batchMode)
    {
        labelProgress.Text = string.Format(LanguageSettings.Current.AudioToText.TranscribingXOfY, 
            _batchFileNumber, listViewInputFiles.Items.Count);
    }
    else
    {
        TaskbarList.SetProgressValue(_parentForm.Handle, 1, 100);
    }

    labelProgress.Refresh();
    Application.DoEvents();
    
    // 读取音频数据
    var buffer = new byte[4096];
    _bytesWavTotal = new FileInfo(waveFileName).Length;
    _bytesWavRead = 0;
    _startTicks = Stopwatch.GetTimestamp();
    timer1.Start();
    
    using (var source = File.OpenRead(waveFileName))
    {
        int bytesRead;
        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            _bytesWavRead += bytesRead;
            
            // 更新进度条
            progressBar1.Value = (int)(_bytesWavRead * 100.0 / _bytesWavTotal);
            progressBar1.Refresh();
            Application.DoEvents();
            
            // 处理音频数据
            if (rec.AcceptWaveform(buffer, bytesRead))
            {
                // 获取完整结果
                var res = rec.Result();
                var results = ParseJsonToResult(res);
                list.AddRange(results);
            }
            else
            {
                // 获取部分结果（用于实时显示）
                var res = rec.PartialResult();
                textBoxLog.AppendText(res.RemoveChar('\r', '\n'));
            }

            // 更新任务栏进度
            if (!_batchMode)
            {
                TaskbarList.SetProgressValue(_parentForm.Handle, 
                    Math.Max(1, progressBar1.Value), progressBar1.Maximum);
            }

            // 检查是否取消
            if (_cancel)
            {
                TaskbarList.SetProgressState(_parentForm.Handle, TaskbarButtonProgressFlags.NoProgress);
                break;
            }
        }

        if (!_batchMode)
        {
            TaskbarList.StartBlink(_parentForm, 10, 1, 2);
        }
    }

    // 获取最终结果
    var finalResult = rec.FinalResult();
    var finalResults = ParseJsonToResult(finalResult);
    list.AddRange(finalResults);
    
    timer1.Stop();
    return list;
}
```

---

### 4. ParseJsonToResult()
**文件**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`
**行号**: 约 332-355

**功能**: 解析 Vosk 返回的 JSON 结果

```csharp
public static List<ResultText> ParseJsonToResult(string result)
{
    var list = new List<ResultText>();
    var jsonParser = new SeJsonParser();
    
    // 获取 result 数组
    var root = jsonParser.GetArrayElementsByName(result, "result");
    
    foreach (var item in root)
    {
        // 解析各个字段
        var conf = jsonParser.GetFirstObject(item, "conf");
        var start = jsonParser.GetFirstObject(item, "start");
        var end = jsonParser.GetFirstObject(item, "end");
        var word = jsonParser.GetFirstObject(item, "word");
        
        // 验证并创建 ResultText 对象
        if (!string.IsNullOrWhiteSpace(word) &&
            decimal.TryParse(conf, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var confidence) &&
            decimal.TryParse(start, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var startSeconds) &&
            decimal.TryParse(end, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var endSeconds))
        {
            var rt = new ResultText 
            { 
                Confidence = confidence, 
                Text = word, 
                Start = startSeconds, 
                End = endSeconds 
            };
            list.Add(rt);
        }
    }

    return list;
}
```

**JSON 示例**:
```json
{
  "result": [
    {
      "conf": 0.9,
      "start": 0.12,
      "end": 0.34,
      "word": "hello"
    },
    {
      "conf": 0.85,
      "start": 0.35,
      "end": 0.56,
      "word": "world"
    }
  ]
}
```

---

### 5. AudioToTextPostProcessor.Fix()
**文件**: `src/libse/AudioToText/AudioToTextPostProcessor.cs`
**行号**: 约 50-120

**功能**: 后处理识别结果，优化字幕质量

```csharp
public Subtitle Fix(Engine engine, List<ResultText> input, bool usePostProcessing, 
    bool addPeriods, bool mergeLines, bool fixCasing, bool fixShortDuration, bool splitLines)
{
    // 将 ResultText 列表转换为 Paragraph 列表
    var subtitle = new Subtitle();
    subtitle.Paragraphs.AddRange(input.Select(p => 
        new Paragraph(p.Text, (double)p.Start * 1000.0, (double)p.End * 1000.0)).ToList());

    return Fix(engine, subtitle, usePostProcessing, addPeriods, mergeLines, 
        fixCasing, fixShortDuration, splitLines);
}

public Subtitle Fix(Engine engine, Subtitle input, bool usePostProcessing, 
    bool addPeriods, bool mergeLines, bool fixCasing, bool fixShortDuration, bool splitLines)
{
    var subtitle = new Subtitle();

    // 过滤无效段落
    for (var index = 0; index < input.Paragraphs.Count; index++)
    {
        var paragraph = input.Paragraphs[index];
        
        // Vosk 特定过滤：过滤过长的 "the"
        if (usePostProcessing && engine == Engine.Vosk && 
            TwoLetterLanguageCode == "en" && paragraph.Text == "the" && 
            paragraph.EndTime.TotalSeconds - paragraph.StartTime.TotalSeconds > 1)
        {
            continue;
        }

        // Whisper 特定过滤
        if (usePostProcessing && engine == Engine.Whisper)
        {
            if (new[] { "(.", "(" }.Contains(paragraph.Text))
            {
                continue;
            }

            if (TwoLetterLanguageCode == "da")
            {
                if (paragraph.Text.Contains("Danske tekster af nicolai winther", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            // 移除多余空格
            paragraph.Text = Utilities.RemoveUnneededSpaces(paragraph.Text, TwoLetterLanguageCode);

            // 修复标点
            if (paragraph.Text.StartsWith('.') && paragraph.Text.EndsWith(").", StringComparison.Ordinal))
            {
                paragraph.Text = paragraph.Text.TrimEnd('.');
            }

            // 修复重叠时间
            var next = input.GetParagraphOrDefault(index + 1);
            if (next != null && Math.Abs(paragraph.EndTime.TotalMilliseconds - next.StartTime.TotalMilliseconds) < 0.01)
            {
                next.StartTime.TotalMilliseconds++;
            }
        }

        subtitle.Paragraphs.Add(paragraph);
    }

    // 丹麦语特定修复
    if (usePostProcessing && engine == Engine.Whisper && TwoLetterLanguageCode == "da")
    {
        new FixDanishLetterI().Fix(subtitle, new EmptyFixCallback());
    }

    return Fix(subtitle, usePostProcessing, addPeriods, mergeLines, fixCasing, 
        fixShortDuration, splitLines, engine);
}
```

---

### 6. Fix() - 后处理主方法
**文件**: `src/libse/AudioToText/AudioToTextPostProcessor.cs`
**行号**: 约 140-180

**功能**: 执行各种后处理优化

```csharp
public Subtitle Fix(Subtitle subtitle, bool usePostProcessing, bool addPeriods, 
    bool mergeLines, bool fixCasing, bool fixShortDuration, bool splitLines, Engine engine)
{
    if (usePostProcessing)
    {
        // 添加句号
        if (addPeriods)
        {
            subtitle = AddPeriods(subtitle, TwoLetterLanguageCode);
        }

        // 修复大小写
        if (fixCasing)
        {
            subtitle = FixCasing(subtitle, TwoLetterLanguageCode, engine);
        }

        // 修复短持续时间
        if (fixShortDuration)
        {
            subtitle = FixShortDuration(subtitle);
        }

        // 分割长行
        if (splitLines && !IsNonStandardLineTerminationLanguage(TwoLetterLanguageCode) && 
            AllowLineContentMove(engine))
        {
            var totalMaxChars = Configuration.Settings.General.SubtitleLineMaximumLength * 
                Configuration.Settings.General.MaxNumberOfLines;
            subtitle = SplitLongLinesHelper.SplitLongLinesInSubtitle(subtitle, totalMaxChars, 
                Configuration.Settings.General.SubtitleLineMaximumLength);
            subtitle = TryForWholeSentences(subtitle, TwoLetterLanguageCode, 
                Configuration.Settings.General.SubtitleLineMaximumLength);
            subtitle = TryForWholeSentences(subtitle, TwoLetterLanguageCode, 
                Configuration.Settings.General.SubtitleLineMaximumLength);
        }

        // 合并短行
        if (mergeLines && AllowLineContentMove(engine))
        {
            subtitle = MergeShortLines(subtitle, TwoLetterLanguageCode);
            subtitle = AutoBalanceLines(subtitle, TwoLetterLanguageCode);
        }
    }

    // 重新编号
    subtitle.Renumber();
    return subtitle;
}
```

---

### 7. AddPeriods()
**文件**: `src/libse/AudioToText/AudioToTextPostProcessor.cs`
**行号**: 约 280-330

**功能**: 在适当位置添加句号

```csharp
public Subtitle AddPeriods(Subtitle inputSubtitle, string language)
{
    // 跳过非标准行终止语言（日语、中文等）
    if (IsNonStandardLineTerminationLanguage(language))
    {
        return new Subtitle(inputSubtitle);
    }

    var englishSkipLastWords = new[] { "with", "however", "a" };
    var englishSkipFirstWords = new[] { "to", "and", "but", "and", "with", "off", "have" };

    var subtitle = new Subtitle(inputSubtitle);
    
    // 遍历所有段落（除最后一个）
    for (var index = 0; index < subtitle.Paragraphs.Count - 1; index++)
    {
        var paragraph = subtitle.Paragraphs[index];
        var next = subtitle.Paragraphs[index + 1];
        
        // 如果下一个字幕距离当前字幕超过阈值，且当前字幕没有结束标点
        if (next.StartTime.TotalMilliseconds - paragraph.EndTime.TotalMilliseconds > 
            SetPeriodIfDistanceToNextIsMoreThan &&
            !paragraph.Text.EndsWith('.') &&
            !paragraph.Text.EndsWith('!') &&
            !paragraph.Text.EndsWith('?') &&
            !paragraph.Text.EndsWith(',') &&
            !paragraph.Text.EndsWith(':') &&
            !paragraph.Text.EndsWith(')') &&
            !paragraph.Text.EndsWith(']') &&
            !paragraph.Text.EndsWith('}'))
        {
            var gap = next.StartTime.TotalMilliseconds - paragraph.EndTime.TotalMilliseconds;
            
            if (gap > SetPeriodIfDistanceToNextIsMoreThanAlways)
            {
                // 距离很大，直接添加句号
                paragraph.Text += ".";
            }
            else
            {
                // 检查最后一个词和下一个字幕的第一个词
                var lastWord = GetLastWord(paragraph.Text);
                var nextFirstWord = GetFirstWord(next.Text);
                
                if (TwoLetterLanguageCode == "en" && 
                    (englishSkipLastWords.Contains(lastWord) || 
                     englishSkipFirstWords.Contains(nextFirstWord)))
                {
                    continue;
                }

                paragraph.Text += ".";
            }
        }
    }

    // 处理最后一个字幕
    var last = subtitle.GetParagraphOrDefault(subtitle.Paragraphs.Count - 1);
    if (last != null &&
        !last.Text.EndsWith('.') &&
        !last.Text.EndsWith('!') &&
        !last.Text.EndsWith('?') &&
        !last.Text.EndsWith(',') &&
        !last.Text.EndsWith(':') &&
        !last.Text.EndsWith(')') &&
        !last.Text.EndsWith(']') &&
        !last.Text.EndsWith('}'))
    {
        subtitle.Paragraphs[subtitle.Paragraphs.Count - 1].Text += ".";
    }

    return subtitle;
}
```

---

### 8. MergeShortLines()
**文件**: `src/libse/AudioToText/AudioToTextPostProcessor.cs`
**行号**: 约 380-480

**功能**: 合并短行

```csharp
public Subtitle MergeShortLines(Subtitle subtitle, string language)
{
    const int maxMillisecondsBetweenLines = 100;
    const bool onlyContinuousLines = true;

    // 根据语言设置最大字符数
    if (language == "jp")
    {
        ParagraphMaxChars = Configuration.Settings.Tools.AudioToTextLineMaxCharsJp;
    }

    if (language == "cn" || language == "yue")
    {
        ParagraphMaxChars = Configuration.Settings.Tools.AudioToTextLineMaxCharsCn;
    }

    var mergedSubtitle = new Subtitle();
    var lastMerged = false;
    Paragraph p = null;
    
    for (var i = 1; i < subtitle.Paragraphs.Count; i++)
    {
        if (!lastMerged)
        {
            p = new Paragraph(subtitle.GetParagraphOrDefault(i - 1));
            mergedSubtitle.Paragraphs.Add(p);
        }

        var next = subtitle.GetParagraphOrDefault(i);
        var nextNext = subtitle.GetParagraphOrDefault(i + 1);
        
        if (next != null)
        {
            // 检查是否可以合并
            if (Utilities.QualifiesForMerge(p, next, maxMillisecondsBetweenLines, 
                ParagraphMaxChars, onlyContinuousLines))
            {
                MergeNextIntoP(language, p, next);
                lastMerged = true;
            }
            else if (IsNextCloseAndAlone(p, next, nextNext, maxMillisecondsBetweenLines, 
                onlyContinuousLines))
            {
                // 尝试分割
                var splitDone = false;
                if (!IsNonStandardLineTerminationLanguage(language))
                {
                    var pNew = new Paragraph(p);
                    MergeNextIntoP(language, pNew, next);
                    var textNoHtml = HtmlUtil.RemoveHtmlTags(pNew.Text, true);
                    var arr = textNoHtml.SplitToLines();
                    
                    foreach (var line in arr)
                    {
                        if (line.Length > Configuration.Settings.General.SubtitleLineMaximumLength)
                        {
                            var text = Utilities.AutoBreakLine(pNew.Text, language);
                            arr = text.SplitToLines();
                            
                            if (arr.Count == 2)
                            {
                                var oldP = new Paragraph(p);

                                p.Text = Utilities.AutoBreakLine(arr[0], language);
                                next.Text = Utilities.AutoBreakLine(arr[1], language);

                                var durationToMove = CalcDurationToMove(oldP, p, next);
                                p.EndTime.TotalMilliseconds += durationToMove;
                                next.StartTime.TotalMilliseconds += durationToMove;

                                splitDone = true;
                            }

                            break;
                        }
                    }
                }

                if (splitDone)
                {
                    lastMerged = false;
                }
                else
                {
                    MergeNextIntoP(language, p, next);
                    lastMerged = true;
                }
            }
            else
            {
                lastMerged = false;
            }
        }
        else
        {
            lastMerged = false;
        }
    }

    if (lastMerged)
    {
        return mergedSubtitle;
    }

    var last = subtitle.GetParagraphOrDefault(subtitle.Paragraphs.Count - 1);
    if (last != null && !string.IsNullOrWhiteSpace(last.Text))
    {
        mergedSubtitle.Paragraphs.Add(new Paragraph(last));
    }

    return mergedSubtitle;
}
```

---

### 9. SubRip.ToText()
**文件**: `src/libse/SubtitleFormats/SubRip.cs`
**行号**: 约 55-65

**功能**: 将 Subtitle 对象转换为 SRT 格式文本

```csharp
public override string ToText(Subtitle subtitle, string title)
{
    const string paragraphWriteFormat = "{0}{4}{1} --> {2}{4}{3}{4}{4}";

    var sb = new StringBuilder();
    
    // 遍历所有段落
    foreach (var p in subtitle.Paragraphs)
    {
        // 格式：序号 + 换行 + 开始时间 --> 结束时间 + 换行 + 文本 + 换行 + 换行
        sb.AppendFormat(paragraphWriteFormat, 
            p.Number,           // 序号
            p.StartTime,        // 开始时间
            p.EndTime,          // 结束时间
            p.Text,             // 文本
            Environment.NewLine);
    }
    
    return sb.ToString().Trim() + Environment.NewLine + Environment.NewLine;
}
```

**SRT 格式示例**:
```
1
00:00:01,000 --> 00:00:04,000
Hello world

2
00:00:05,000 --> 00:00:08,000
This is a subtitle
```

---

### 10. SubRip.LoadSubtitle()
**文件**: `src/libse/SubtitleFormats/SubRip.cs`
**行号**: 约 70-140

**功能**: 从 SRT 文本加载到 Subtitle 对象

```csharp
public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
{
    var doRenumber = false;
    _errors = new StringBuilder();
    _lineNumber = 0;
    _isMsFrames = true;
    _isWsrt = fileName != null && fileName.EndsWith(".wsrt", StringComparison.OrdinalIgnoreCase);
    _paragraph = new Paragraph();
    _expecting = ExpectingLine.Number;
    _errorCount = 0;

    subtitle.Paragraphs.Clear();
    
    var line = string.Empty;
    var next = lines.Count > 0 ? lines[0].TrimEnd().Trim('\u007F') : string.Empty;
    var nextNext = lines.Count > 1 ? lines[1].TrimEnd().Trim('\u007F') : string.Empty;
    var nextNextNext = lines.Count > 2 ? lines[2].TrimEnd().Trim('\u007F') : string.Empty;
    
    // 逐行解析
    for (var i = 0; i < lines.Count; i++)
    {
        _lineNumber++;
        line = next;
        next = nextNext;
        nextNext = nextNextNext;
        nextNextNext = (i + 3 < lines.Count) ? lines[i + 3].TrimEnd().Trim('\u007F') : string.Empty;

        // 处理缺少空行的情况
        if (_expecting == ExpectingLine.Text && i + 1 < lines.Count && 
            !string.IsNullOrEmpty(_paragraph?.Text) &&
            Utilities.IsInteger(line) && TryReadTimeCodesLine(line.Trim(), null, false))
        {
            if (!string.IsNullOrEmpty(_paragraph.Text))
            {
                subtitle.Paragraphs.Add(_paragraph);
                _lastParagraph = _paragraph;
                _paragraph = new Paragraph();
            }
            _expecting = ExpectingLine.Number;
        }

        // 解析序号
        if (_expecting == ExpectingLine.Number && TryReadTimeCodesLine(line.Trim(), null, false))
        {
            _expecting = ExpectingLine.TimeCodes;
            doRenumber = true;
        }
        // 解析时间码
        else if (!string.IsNullOrEmpty(_paragraph?.Text) && _expecting == ExpectingLine.Text && 
                 TryReadTimeCodesLine(line.Trim(), null, false))
        {
            subtitle.Paragraphs.Add(_paragraph);
            _lastParagraph = _paragraph;
            _paragraph = new Paragraph();
            _expecting = ExpectingLine.TimeCodes;
            doRenumber = true;
        }

        ReadLine(subtitle, line, next, nextNext, nextNextNext);
    }

    // 添加最后一个段落
    if (_paragraph?.IsDefault == false || _paragraph != null && _expecting == ExpectingLine.Text)
    {
        subtitle.Paragraphs.Add(_paragraph);
    }

    // 重新编号
    if (doRenumber)
    {
        subtitle.Renumber();
    }

    // 处理帧数/毫秒
    foreach (var p in subtitle.Paragraphs)
    {
        if (_isMsFrames)
        {
            p.StartTime.Milliseconds = FramesToMillisecondsMax999(p.StartTime.Milliseconds);
            p.EndTime.Milliseconds = FramesToMillisecondsMax999(p.EndTime.Milliseconds);
        }
        p.Text = p.Text.TrimEnd();
    }

    Errors = _errors.ToString();
}
```

---

### 11. SaveToSourceFolder()
**文件**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`
**行号**: 约 210-250

**功能**: 保存字幕到源文件夹

```csharp
private void SaveToSourceFolder(string videoFileName)
{
    // 获取默认格式
    var format = SubtitleFormat.FromName(Configuration.Settings.General.DefaultSubtitleFormat, new SubRip());
    
    // 转换为文本
    var text = TranscribedSubtitle.ToText(format);

    // 生成文件名
    var fileName = Path.Combine(Utilities.GetPathAndFileNameWithoutExtension(videoFileName)) + format.Extension;
    
    // 如果文件已存在，添加 GUID
    if (File.Exists(fileName))
    {
        fileName = $"{Path.Combine(Utilities.GetPathAndFileNameWithoutExtension(videoFileName))}." +
            $"{Guid.NewGuid().ToString()}.{format.Extension}";
    }

    try
    {
        // 写入文件
        File.WriteAllText(fileName, text, Encoding.UTF8);
        textBoxLog.AppendText("Subtitle written to : " + fileName + Environment.NewLine);
        _outputBatchFileNames.Add(fileName);
    }
    catch
    {
        var dir = Path.GetDirectoryName(fileName);
        if (!FileUtil.IsDirectoryWritable(dir))
        {
            MessageBox.Show(this, $"SE does not have write access to the folder '{dir}'", MessageBoxIcon.Error);
        }

        throw;
    }
}
```

---

## VoskAudioToTextSelectedLines 特殊方法

### TranscribeViaVosk() - 选中行版本
**文件**: `src/ui/Forms/AudioToText/VoskAudioToTextSelectedLines.cs`
**行号**: 约 200-260

**功能**: 对选中行进行 Vosk 识别（与主版本类似，但简化了进度显示）

```csharp
public List<ResultText> TranscribeViaVosk(string waveFileName, string modelFileName)
{
    Directory.SetCurrentDirectory(_voskFolder);
    Vosk.Vosk.SetLogLevel(0);
    
    if (_model == null)
    {
        labelProgress.Text = LanguageSettings.Current.AudioToText.LoadingVoskModel;
        labelProgress.Refresh();
        Application.DoEvents();
        _model = new Model(modelFileName);
    }
    
    var rec = new VoskRecognizer(_model, 16000.0f);
    rec.SetMaxAlternatives(0);
    rec.SetWords(true);
    var list = new List<ResultText>();
    
    labelProgress.Text = LanguageSettings.Current.AudioToText.Transcribing;
    labelProgress.Text = string.Format(LanguageSettings.Current.AudioToText.TranscribingXOfY, 
        _batchFileNumber, listViewInputFiles.Items.Count);
    labelProgress.Refresh();
    Application.DoEvents();
    
    var buffer = new byte[4096];
    _bytesWavTotal = new FileInfo(waveFileName).Length;
    _bytesWavRead = 0;
    _startTicks = Stopwatch.GetTimestamp();
    timer1.Start();
    
    using (var source = File.OpenRead(waveFileName))
    {
        int bytesRead;
        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            _bytesWavRead += bytesRead;
            
            if (rec.AcceptWaveform(buffer, bytesRead))
            {
                var res = rec.Result();
                var results = VoskAudioToText.ParseJsonToResult(res);
                list.AddRange(results);
            }
            else
            {
                var res = rec.PartialResult();
                textBoxLog.AppendText(res.RemoveChar('\r', '\n'));
            }

            if (_cancel)
            {
                TaskbarList.SetProgressState(_parentForm.Handle, TaskbarButtonProgressFlags.NoProgress);
                return null;
            }
        }
    }

    var finalResult = rec.FinalResult();
    var finalResults = VoskAudioToText.ParseJsonToResult(finalResult);
    list.AddRange(finalResults);
    
    timer1.Stop();
    return list;
}
```

---

## 批量模式处理

### GenerateBatch()
**文件**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`
**行号**: 约 122-210

**功能**: 批量处理多个文件

```csharp
private void GenerateBatch()
{
    groupBoxInputFiles.Enabled = false;
    _batchFileNumber = 0;
    var errors = new StringBuilder();
    var errorCount = 0;
    
    textBoxLog.AppendText("Batch mode" + Environment.NewLine);
    
    foreach (ListViewItem lvi in listViewInputFiles.Items)
    {
        _batchFileNumber++;
        var videoFileName = lvi.Text;
        listViewInputFiles.SelectedIndices.Clear();
        lvi.Selected = true;
        
        ShowProgressBar();
        var modelFileName = Path.Combine(_voskFolder, comboBoxModels.Text);
        
        buttonGenerate.Enabled = false;
        buttonDownload.Enabled = false;
        buttonBatchMode.Enabled = false;
        comboBoxModels.Enabled = false;
        
        var waveFileName = GenerateWavFile(videoFileName, _audioTrackNumber);
        
        if (!File.Exists(waveFileName))
        {
            errors.AppendLine("Unable to extract audio from: " + videoFileName);
            errorCount++;
            continue;
        }

        progressBar1.Style = ProgressBarStyle.Blocks;
        var transcript = TranscribeViaVosk(waveFileName, modelFileName);
        
        if (_cancel)
        {
            TaskbarList.SetProgressState(_parentForm.Handle, TaskbarButtonProgressFlags.NoProgress);
            if (!_batchMode)
            {
                DialogResult = DialogResult.Cancel;
            }

            groupBoxInputFiles.Enabled = true;
            return;
        }

        var postProcessor = new AudioToTextPostProcessor(GetLanguage(comboBoxModels.Text))
        {
            ParagraphMaxChars = Configuration.Settings.General.SubtitleLineMaximumLength * 2,
        };
        TranscribedSubtitle = postProcessor.Fix(AudioToTextPostProcessor.Engine.Vosk, 
            transcript, checkBoxUsePostProcessing.Checked, true, true, true, true, false);

        SaveToSourceFolder(videoFileName);
        TaskbarList.SetProgressValue(_parentForm.Handle, _batchFileNumber, listViewInputFiles.Items.Count);
    }

    progressBar1.Visible = false;
    labelTime.Text = string.Empty;

    TaskbarList.StartBlink(_parentForm, 10, 1, 2);

    if (errors.Length > 0)
    {
        MessageBox.Show(this, $"{errorCount} error(s)!{Environment.NewLine}{errors}");
    }

    var fileList = Environment.NewLine + Environment.NewLine + 
        string.Join(Environment.NewLine, _outputBatchFileNames);
    MessageBox.Show(this, string.Format(LanguageSettings.Current.AudioToText.XFilesSavedToVideoSourceFolder, 
        listViewInputFiles.Items.Count - errorCount) + fileList);

    groupBoxInputFiles.Enabled = true;
    buttonGenerate.Enabled = true;
    buttonDownload.Enabled = true;
    buttonBatchMode.Enabled = true;
    DialogResult = DialogResult.Cancel;
}
```

---

## 辅助方法

### GetLanguage()
**文件**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`
**行号**: 约 257-275

**功能**: 从模型名称中提取语言代码

```csharp
internal static string GetLanguage(string text)
{
    var languageCodeList = VoskModel.Models.Select(p => p.TwoLetterLanguageCode);
    
    foreach (var languageCode in languageCodeList)
    {
        if (text.Contains("model-" + languageCode) || 
            text.Contains("model-small-" + languageCode) || 
            text.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase))
        {
            return languageCode;
        }

        if (languageCode == "jp" && (text.Contains("model-ja") || text.Contains("model-small-ja")))
        {
            return languageCode;
        }
    }

    return "en";
}
```

### FillModels()
**文件**: `src/ui/Forms/AudioToText/VoskAudioToText.cs`
**行号**: 约 60-77

**功能**: 填充模型下拉列表

```csharp
public static void FillModels(NikseComboBox comboBoxModels, string lastDownloadedModel)
{
    var voskFolder = Path.Combine(Configuration.DataDirectory, "Vosk");
    var selectName = string.IsNullOrEmpty(lastDownloadedModel) ? 
        Configuration.Settings.Tools.VoskModel : lastDownloadedModel;
    
    comboBoxModels.Items.Clear();
    
    foreach (var directory in Directory.GetDirectories(voskFolder))
    {
        var name = Path.GetFileName(directory);
        
        // 检查模型文件是否存在
        if (!File.Exists(Path.Combine(directory, "final.mdl")) && 
            !File.Exists(Path.Combine(directory, "am", "final.mdl")))
        {
            continue;
        }

        comboBoxModels.Items.Add(name);
        
        if (name == selectName)
        {
            comboBoxModels.SelectedIndex = comboBoxModels.Items.Count - 1;
        }
    }

    if (comboBoxModels.SelectedIndex < 0 && comboBoxModels.Items.Count > 0)
    {
        comboBoxModels.SelectedIndex = 0;
    }
}
```

---

## SRT 结果输出逻辑总结

### 完整流程

1. **音频提取** (`GenerateWavFile`)
   - 使用 FFmpeg 从视频中提取音频
   - 转换为 16kHz WAV 格式
   - 保存到临时文件

2. **Vosk 识别** (`TranscribeViaVosk`)
   - 加载 Vosk 模型
   - 创建 VoskRecognizer
   - 逐块读取音频数据
   - 调用 `AcceptWaveform()` 处理音频
   - 调用 `Result()` 获取识别结果
   - 调用 `FinalResult()` 获取最终结果

3. **JSON 解析** (`ParseJsonToResult`)
   - 解析 Vosk 返回的 JSON
   - 提取 `result` 数组
   - 解析每个词的 `conf`, `start`, `end`, `word`
   - 创建 `ResultText` 对象列表

4. **后处理** (`AudioToTextPostProcessor.Fix`)
   - 过滤无效段落
   - 添加句号 (`AddPeriods`)
   - 修复大小写 (`FixCasing`)
   - 修复短持续时间 (`FixShortDuration`)
   - 分割长行 (`SplitLongLinesHelper`)
   - 合并短行 (`MergeShortLines`)
   - 自动平衡行 (`AutoBalanceLines`)

5. **转换为 SRT** (`SubRip.ToText`)
   - 遍历所有 Paragraph
   - 格式化为 SRT 格式
   - 输出文本字符串

6. **保存文件** (`SaveToSourceFolder`)
   - 生成文件名
   - 写入 UTF-8 编码的文本文件
   - 保存到源文件夹

---

## 关键技术点

### 1. Vosk API 使用
- `Model`: 加载声学模型
- `VoskRecognizer`: 创建识别器
- `AcceptWaveform()`: 处理音频数据
- `Result()`: 获取完整识别结果
- `PartialResult()`: 获取部分结果（用于实时显示）
- `FinalResult()`: 获取最终结果

### 2. 时间码处理
- Vosk 返回的时间单位为秒
- SRT 格式使用 `HH:MM:SS,mmm` 格式
- 需要转换：秒 → 毫秒 → TimeCode

### 3. 后处理策略
- 基于时间间隔添加标点
- 基于字符数分割/合并行
- 基于语言特性进行优化
- 过滤低质量识别结果

### 4. 错误处理
- 检查模型文件是否存在
- 检查音频文件是否生成成功
- 处理用户取消操作
- 检查磁盘空间

---

## 配置项

### Configuration.Settings 相关
- `General.FFmpegLocation`: FFmpeg 路径
- `General.FFmpegUseCenterChannelOnly`: 是否仅使用中心声道
- `General.SubtitleLineMaximumLength`: 字幕行最大长度
- `General.MaxNumberOfLines`: 最大行数
- `General.DefaultSubtitleFormat`: 默认字幕格式
- `Tools.VoskModel`: Vosk 模型名称
- `Tools.VoskPostProcessing`: 是否使用后处理
- `Tools.AudioToTextLineMaxChars`: 音频转文本行最大字符数
- `Tools.AudioToTextLineMaxCharsJp`: 日语行最大字符数
- `Tools.AudioToTextLineMaxCharsCn`: 中文行最大字符数

---

## 支持的语言

VoskModel.cs 中定义了支持的语言模型：
- 英语 (en) - 中等/大/超大
- 中文 (cn) - 小/大
- 法语 (fr) - 小/大
- 西班牙语 (es) - 小/大
- 韩语 (ko) - 小
- 德语 (de) - 小/大
- 葡萄牙语 (pt) - 小/大
- 意大利语 (it) - 小/大
- 荷兰语 (nl) - 大
- 瑞典语 (sv) - 小
- 俄语 (ru) - 小/大
- 波斯语 (fa) - 小
- 土耳其语 (tr) - 小
- 希腊语 (el) - 小
- 阿拉伯语 (ar) - 小/大
- 乌克兰语 (uk) - 小/中
- 乌兹别克语 (uz) - 小
- 菲律宾语 (ph) - 中
- 哈萨克语 (kz) - 小
- 日语 (jp) - 小/大
- 加泰罗尼亚语 (ca) - 小
- 印地语 (hi) - 小/大
- 捷克语 (cz) - 小
- 波兰语 (pl) - 小
- 布列塔尼语 (br) - 小

---

## 总结

SubtitleEdit 的 Vosk 识别功能实现了一个完整的音频转字幕流程：

1. **音频预处理**: 使用 FFmpeg 提取音频并转换为 WAV 格式
2. **语音识别**: 使用 Vosk 引擎进行离线语音识别
3. **结果解析**: 解析 JSON 格式的识别结果
4. **后处理优化**: 通过多种策略优化字幕质量
5. **格式转换**: 转换为标准的 SRT 格式
6. **文件保存**: 保存到指定位置

整个过程模块化设计良好，各个职责分离清晰，易于维护和扩展。
