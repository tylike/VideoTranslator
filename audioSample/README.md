# 音频识别整合系统

这是一个整合多个语音识别引擎的 C# 应用程序，结合了 Whisper 和 Sherpa-ONNX 的优势，提供更准确的语音识别结果。

## 功能特点

### 整合多引擎优势

1. **Whisper.cpp** - 提供准确的语音识别内容
2. **Sherpa-ONNX VAD** - 提供精确的静音检测和时间边界
3. **Sherpa-ONNX 说话人识别** - 识别不同的说话人
4. **Sherpa-ONNX 词级时间戳** - 提供细粒度的词级时间对齐

### 整合策略

- 以 Whisper 的文本内容为基础，保证识别准确性
- 使用 Sherpa-ONNX 的 VAD 结果调整时间边界，提高静音检测精度
- 使用 Sherpa-ONNX 的说话人识别结果添加说话人标签
- 使用 Sherpa-ONNX 的词级时间戳进行细粒度对齐

## 项目结构

```
audioSample/
├── Models/                    # 数据模型
│   └── AudioModels.cs        # 音频相关数据模型
├── Utils/                    # 工具类
│   └── AudioUtils.cs         # 音频处理工具
├── Parsers/                  # 解析器
│   ├── WhisperParser.cs      # Whisper 结果解析
│   └── SherpaParser.cs      # Sherpa-ONNX 结果解析
├── Integrators/              # 整合器
│   └── TranscriptionIntegrator.cs  # 结果整合算法
├── Services/                 # 服务层
│   └── TranscriptionService.cs      # 语音识别服务
└── Program.cs               # 主程序入口
```

## 使用方法

### 基本使用

```bash
cd d:\VideoTranslator\audioSample
dotnet run
```

### 指定音频文件

```bash
dotnet run -- "path\to\your\audio.flac"
```

### 配置选项

在 [Program.cs](Program.cs) 中可以配置以下选项：

- `useVAD` - 是否使用 VAD 静音检测（默认：true）
- `useSpeakerDiarization` - 是否使用说话人识别（默认：false，需要额外模型）
- `useWordTimestamps` - 是否使用词级时间戳（默认：true）

### 配置路径

在 [TranscriptionService.cs](Services/TranscriptionService.cs) 中配置各个工具的路径：

```csharp
public string WhisperPath { get; set; } = @"d:\VideoTranslator\whisper.cpp\main.exe";
public string WhisperModelPath { get; set; } = @"d:\VideoTranslator\whisper.cpp\ggml-large-v3.bin";
public string SherpaBinPath { get; set; } = @"d:\VideoTranslator\第三方项目\stt\sherpa-onnx-exe-v1.12.23\bin";
// ... 其他路径配置
```

## 输出格式

程序会在音频文件所在目录生成以下文件：

1. `{filename}_integrated.srt` - SRT 格式字幕文件
2. `{filename}_integrated.json` - JSON 格式详细结果
3. `{filename}_integrated.txt` - 纯文本格式
4. `{filename}_integrated_detailed.txt` - 包含词级时间戳的详细文本

### SRT 格式示例

```
1
[00:00:00.670 --> 00:00:02.240]
这是第一段识别的文本

2
[00:00:03.490 --> 00:00:04.570]
这是第二段识别的文本
```

### JSON 格式示例

```json
{
  "fullText": "完整的识别文本",
  "segments": [
    {
      "startTime": 0.67,
      "endTime": 2.24,
      "text": "识别的文本",
      "speakerId": 0,
      "wordTimestamps": [
        {
          "word": "识别",
          "startTime": 0.67,
          "endTime": 0.89
        }
      ]
    }
  ],
  "speakerCount": 2,
  "totalDuration": 160.19
}
```

## 依赖项

- .NET 10.0
- NAudio (音频处理库)
- Whisper.cpp (语音识别引擎)
- Sherpa-ONNX (语音识别引擎)

## 编译

```bash
cd d:\VideoTranslator\audioSample
dotnet build
```

## 运行

```bash
dotnet run
```

## 注意事项

1. 确保 Whisper.cpp 和 Sherpa-ONNX 的可执行文件路径配置正确
2. 确保模型文件存在于指定路径
3. 首次运行可能需要较长时间加载模型
4. 说话人识别需要额外的说话人嵌入模型（目前未配置）

## 性能优化建议

1. 使用 GPU 加速（如果可用）
2. 根据需要选择启用/禁用某些功能
3. 对于长音频，考虑分段处理

## 故障排除

### Whisper 执行失败

检查 Whisper 路径和模型路径是否正确。

### Sherpa-ONNX 执行失败

检查 Sherpa-ONNX 可执行文件和模型路径是否正确。

### FFmpeg 未找到

如果需要音频格式转换，确保 FFmpeg 在系统 PATH 中。

## 许可证

本项目整合了以下开源项目：
- Whisper.cpp (MIT License)
- Sherpa-ONNX (Apache 2.0 License)
- NAudio (MIT License)
