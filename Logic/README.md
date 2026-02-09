# VideoTranslate

C# 视频翻译工具，支持从 YouTube 导入视频、下载字幕和音频，通过完整的语音识别、翻译、TTS 和音频对齐流程，实现英文转中文的视频转换。

## 项目概述

VideoTranslate 是一个功能完整的视频翻译解决方案，提供从视频导入到最终导出的全流程支持。项目采用模块化设计，支持多种翻译 API 和 TTS 服务，能够智能对齐中英文音频，生成高质量的翻译视频。

## 核心功能

### 1. 视频导入
- **YouTube 导入**：支持从 YouTube URL 下载视频、音频和字幕
- **本地导入**：支持从本地视频文件导入
- **视频信息查询**：获取 YouTube 视频的详细信息、可用字幕和音频格式

### 2. 语音识别
- **Whisper.cpp 集成**：使用 Whisper.cpp 进行高精度语音识别
- **多语言支持**：支持识别多种语言的音频
- **自动字幕生成**：自动生成 SRT 格式字幕文件
- **模型自动选择**：自动选择可用的 Whisper 模型（large-v3 > large-v2 > large > medium > small > base）

### 3. 字幕翻译
- **多翻译引擎**：
  - LMStudio（本地大模型翻译）
  - Google 翻译
  - Bing 翻译
  - DeepL 翻译
- **批量翻译**：支持批量翻译字幕，提高效率
- **时间轴保留**：翻译后保留原始字幕的时间轴信息

### 4. TTS 音频生成
- **语音克隆**：基于源音频进行语音克隆，保持说话人特征
- **情感控制**：支持情感模式（emo_mode）和情感权重（emo_weight）调节
- **GPU 加速**：支持指定 GPU ID 进行加速
- **断点续传**：自动跳过已生成的音频片段
- **清理功能**：支持清理旧的音频文件重新生成

### 5. 智能音频对齐
- **时间轴对齐**：根据英文字幕时间轴精确对齐中文音频
- **自动速度调整**：当中文音频时长与英文不匹配时，自动调整播放速度
- **智能静音填充**：
  - 前置静音：在音频片段前添加静音以匹配开始时间
  - 后置静音：在音频片段后添加静音以匹配结束时间
- **多级速度调整**：支持 0.5x - 2.0x 的速度调整范围
- **音频合并**：将所有处理后的音频片段合并为完整音频

### 6. 测试模式
- **翻译数量限制**：支持限制处理的字幕数量，用于快速测试
  - 设置为 -1：处理全部字幕（默认）
  - 设置为具体数字（如 20）：只处理前 N 条字幕
  - 适用于：翻译、TTS 生成、音频对齐等所有步骤
- **快速验证**：无需处理完整视频即可验证流程和效果

### 7. 视频导出
- **完整视频导出**：导出包含翻译音频的完整视频
- **纯音频导出**：仅导出处理后的音频文件
- **字幕嵌入**：支持将字幕嵌入到视频中

## 项目结构

```
VideoTranslate/
├── Config/                    # 配置文件
│   ├── AppSettings.cs         # 配置类
│   └── appsettings.json      # 应用配置
├── Interfaces/               # 接口定义
│   ├── IAudioService.cs
│   ├── IFFmpegService.cs
│   ├── IPathService.cs
│   ├── ISubtitleService.cs
│   ├── IYouTubeService.cs     # YouTube 下载服务
│   ├── ITrackService.cs       # 轨道管理
│   └── IVideoEditorService.cs # 视频编辑器
├── Models/                   # 数据模型
│   ├── AudioInfo.cs
│   ├── AudioSegment.cs
│   ├── AudioTrack.cs         # 音频轨道
│   ├── Subtitle.cs
│   ├── SubtitleTrack.cs      # 字幕轨道
│   ├── VideoEditor.cs        # 视频编辑器
│   ├── VideoProject.cs
│   └── YouTubeVideo.cs       # YouTube 视频信息
├── Services/                 # 服务实现
│   ├── AudioOverlayService.cs    # 音频覆盖主服务
│   ├── AudioService.cs          # 音频处理服务
│   ├── FFmpegService.cs         # FFmpeg 调用服务
│   ├── PathService.cs           # 路径管理服务
│   ├── SubtitleService.cs      # 字幕解析服务
│   ├── YouTubeService.cs        # YouTube 下载服务
│   ├── TrackService.cs          # 轨道管理服务
│   └── VideoEditorService.cs    # 视频编辑器服务
├── Utils/                    # 工具类
│   └── ServiceCollectionExtensions.cs
├── Program.cs                # 主程序入口
└── VideoTranslate.csproj     # 项目文件

VideoTranslate.Tests/          # 测试项目（独立目录）
├── SubtitleTests.cs
├── PathServiceTests.cs
├── SubtitleServiceTests.cs
├── AudioTrackTests.cs
├── SubtitleTrackTests.cs
└── VideoEditorTests.cs
```

## 功能特性

### 视频编辑器
- **从 YouTube 导入**：支持从 YouTube URL 导入视频、音频和字幕
- **从本地导入**：支持从本地视频文件导入
- **轨道系统**：
  - 音频轨道（源音频、翻译音频、背景音乐、音效）
  - 字幕轨道（源字幕、翻译字幕、自定义字幕）
- **项目管理**：支持保存和加载项目（JSON 格式）
- **导出功能**：支持导出视频或仅导出音频

### 音频处理
- **字幕解析**：支持 SRT 格式字幕解析
- **音频信息获取**：获取音频时长、采样率、声道数
- **静音生成**：生成指定时长的静音音频
- **速度调整**：调整音频播放速度（支持多级调整）
- **音频合并**：合并多个音频片段

### 智能对齐
- 根据英文字幕时间轴对齐中文音频
- 自动添加前置/后置静音
- 自动调整中文音频速度以匹配英文时长

## 使用方法

### 编译项目

```bash
cd VideoTranslate
dotnet build
```

### 一键完整翻译

**最简单的方式 - 一条命令完成全部流程：**

```bash
VideoTranslate.exe full <source> <source_path> <project_name> [options]
```

**从 YouTube 一键翻译：**
```bash
VideoTranslate.exe full youtube https://youtube.com/watch?v=xxx MyProject
```

**从本地视频一键翻译：**
```bash
VideoTranslate.exe full local C:\videos\myvideo.mp4 MyProject
```

**带参数的一键翻译：**
```bash
# 只翻译前 20 条字幕（测试模式）
VideoTranslate.exe full youtube https://youtube.com/watch?v=xxx MyProject --limit 20

# 使用 Google 翻译
VideoTranslate.exe full youtube https://youtube.com/watch?v=xxx MyProject --api google

# 指定输出路径
VideoTranslate.exe full local C:\video.mp4 MyProject --output C:\output\final.mp4
```

**参数说明：**
- `<source>` - 视频来源：`youtube` 或 `local`（必需）
- `<source_path>` - YouTube URL 或本地视频路径（必需）
- `<project_name>` - 项目名称（必需）
- `--source-lang` - 源语言代码，默认为 `en`（可选）
- `--target-lang` - 目标语言代码，默认为 `zh`（可选）
- `--api` - 翻译 API，默认为 `lmstudio`（可选）
- `--batch-size` - 批量翻译大小，默认为 10（可选）
- `--limit` - 限制处理的字幕数量，-1 表示全部（可选）
- `--tts-url` - TTS API 地址（可选）
- `--gpu` - GPU ID，默认为 `0`（可选）
- `--output` - 输出视频路径（可选）

`full` 命令会自动执行以下 6 个步骤：
1. 导入视频
2. 语音识别
3. 翻译字幕
4. 生成 TTS 音频
5. 音频对齐
6. 导出视频

### 分步执行（高级用法）

#### 步骤 1：导入视频

**从 YouTube 导入：**
```bash
VideoTranslate.exe youtube <youtube_url> <project_name>

# 示例
VideoTranslate.exe youtube https://youtube.com/watch?v=xxx MyProject
```

**从本地导入：**
```bash
VideoTranslate.exe local <video_path> <project_name>

# 示例
VideoTranslate.exe local C:\videos\myvideo.mp4 MyProject
```

**查看 YouTube 视频信息：**
```bash
VideoTranslate.exe info <youtube_url>

# 示例
VideoTranslate.exe info https://youtube.com/watch?v=xxx
```

#### 步骤 2：语音识别

```bash
VideoTranslate.exe recognize <project_file> <language>

# 示例：识别英文音频
VideoTranslate.exe recognize MyProject.json en
```

此步骤会使用 Whisper.cpp 进行语音识别，生成英文字幕文件。

#### 步骤 3：翻译字幕

```bash
VideoTranslate.exe translate <json_file> <source_lang> <target_lang> [--api <api_name>] [--batch-size <size>]

# 使用 LMStudio 翻译（默认）
VideoTranslate.exe translate MyProject.json en zh

# 使用 Google 翻译
VideoTranslate.exe translate MyProject.json en zh --api google

# 使用批量大小为 5
VideoTranslate.exe translate MyProject.json en zh --batch-size 5
```

支持的翻译 API：
- `lmstudio`：本地大模型翻译（默认）
- `google`：Google 翻译
- `bing`：Bing 翻译
- `deepl`：DeepL 翻译

#### 步骤 4：生成 TTS 音频

```bash
VideoTranslate.exe tts <json_file> [--url <tts_url>] [--gpu <gpu_id>] [--clean]

# 使用默认设置
VideoTranslate.exe tts MyProject.json

# 指定 GPU ID
VideoTranslate.exe tts MyProject.json --gpu 1

# 清理旧音频文件重新生成
VideoTranslate.exe tts MyProject.json --clean

# 自定义 TTS API 地址
VideoTranslate.exe tts MyProject.json --url http://192.168.1.100:8000/generate
```

此步骤会：
- 基于源音频进行语音克隆
- 使用翻译后的字幕生成中文音频
- 自动跳过已存在的音频片段

#### 步骤 5：音频对齐和合并

```bash
VideoTranslate.exe overlay <video_id> [--delete-files]

# 使用默认视频 ID
VideoTranslate.exe overlay

# 指定视频 ID
VideoTranslate.exe overlay --id 001

# 处理完成后删除临时文件
VideoTranslate.exe overlay --id 001 --delete-files
```

此步骤会：
- 根据英文字幕时间轴对齐中文音频
- 自动调整音频速度和添加静音
- 合并所有音频片段为完整音频

#### 步骤 6：导出最终视频

```bash
VideoTranslate.exe export <project_file> <output_path> [--audio-only]

# 导出完整视频
VideoTranslate.exe export MyProject.json output.mp4

# 仅导出音频
VideoTranslate.exe export MyProject.json output.wav --audio-only
```

**添加字幕到视频：**
```bash
VideoTranslate.exe add-subtitles <project_file> <subtitle_path> <output_path>

# 示例
VideoTranslate.exe add-subtitles MyProject.json subtitle.srt output.mp4
```

### 快速开始示例

**最简单的方式 - 一条命令完成全部流程：**

```bash
# 从 YouTube 翻译（默认参数）
VideoTranslate.exe full youtube https://youtube.com/watch?v=xxx MyProject

# 从本地视频翻译（默认参数）
VideoTranslate.exe full local C:\videos\myvideo.mp4 MyProject

# 测试模式 - 只翻译前 20 条字幕
VideoTranslate.exe full youtube https://youtube.com/watch?v=xxx MyProject --limit 20
```

**分步执行 - 高级用法：**

完整的翻译流程示例：

```bash
# 1. 导入 YouTube 视频
VideoTranslate.exe youtube https://youtube.com/watch?v=xxx MyProject

# 2. 语音识别
VideoTranslate.exe recognize MyProject.json en

# 3. 翻译字幕
VideoTranslate.exe translate MyProject_updated.json en zh --api lmstudio

# 4. 生成 TTS 音频
VideoTranslate.exe tts MyProject_updated_updated.json --gpu 0

# 5. 音频对齐
VideoTranslate.exe overlay --id 001

# 6. 导出视频
VideoTranslate.exe export MyProject_updated_updated_updated.json final_output.mp4
```

## 测试模式

测试模式允许你只处理部分字幕，用于快速验证翻译流程和效果，无需处理整个视频。

### 使用方法

在 `translate`、`tts` 和 `overlay` 命令中添加 `--limit` 参数：

```bash
# 只翻译前 20 条字幕
VideoTranslate.exe translate MyProject.json en zh --limit 20

# 只生成前 20 条音频
VideoTranslate.exe tts MyProject.json --limit 20

# 只对齐前 20 条音频
VideoTranslate.exe overlay --id 001 --limit 20
```

### 完整测试流程

```bash
# 1. 导入视频
VideoTranslate.exe youtube https://youtube.com/watch?v=xxx MyProject

# 2. 语音识别（识别全部字幕）
VideoTranslate.exe recognize MyProject.json en

# 3. 翻译前 20 条字幕
VideoTranslate.exe translate MyProject.json en zh --limit 20

# 4. 生成前 20 条音频
VideoTranslate.exe tts MyProject_updated.json --limit 20

# 5. 对齐前 20 条音频
VideoTranslate.exe overlay --id 001 --limit 20

# 6. 导出测试视频
VideoTranslate.exe export MyProject_updated_updated.json test_output.mp4
```

### 参数说明

- `--limit -1`：处理全部字幕（默认值）
- `--limit 20`：只处理前 20 条字幕
- `--limit 100`：只处理前 100 条字幕

### 注意事项

1. **限制数量的一致性**：建议在翻译、TTS 和对齐步骤中使用相同的 `--limit` 值
2. **项目文件保存**：`TranslationLimit` 值会保存到项目文件中，方便后续步骤使用
3. **测试完成后**：如果测试效果满意，可以去掉 `--limit` 参数处理完整视频
4. **时间轴保持**：即使只处理部分字幕，时间轴信息也会保持完整

### 适用场景

- **首次使用**：快速了解整个翻译流程
- **参数调优**：测试不同的翻译 API、TTS 参数等
- **质量验证**：验证翻译质量和音频效果
- **性能测试**：测试处理速度和资源消耗

## 配置文件

编辑 `Config/appsettings.json`：

```json
{
  "FfmpegPath": "D:\\index-tts\\ffmpeg\\ffmpeg.exe",
  "YtDlpPath": "yt-dlp",
  "BaseDirectory": "d:\\index-tts\\Audio.En2Cn\\projects",
  "KeepFiles": true
}
```

**配置说明：**
- `FfmpegPath`: FFmpeg 可执行文件路径（用于音频/视频处理）
- `YtDlpPath`: yt-dlp 可执行文件路径（用于 YouTube 下载）
- `BaseDirectory`: 项目基础目录（所有项目文件将保存在此目录下）
- `KeepFiles`: 是否保留处理过程中的临时文件

**Whisper.cpp 配置：**
Whisper.cpp 的路径在 [SpeechRecognitionService.cs](file:///d:/index-tts/VideoTranslate/Services/SpeechRecognitionService.cs#L8-L9) 中配置：
- 默认路径：`d:\index-tts\Audio.En2Cn\whisper.cpp\whisper-cli.exe`
- 模型目录：`d:\index-tts\Audio.En2Cn\whisper.cpp`

**TTS 服务配置：**
TTS 服务的 URL 在运行时通过命令行参数指定，默认为：`http://127.0.0.1:8000/generate`

## 依赖项

### 必需工具
- **.NET 8.0**：运行时环境
- **FFmpeg**：音频和视频处理工具（需要添加到系统 PATH 或在配置中指定完整路径）
- **yt-dlp**：YouTube 视频下载工具（需要添加到系统 PATH）
- **Whisper.cpp**：语音识别引擎，需要下载模型文件（.pt、.bin 或 .gguf 格式）

### TTS 服务
- **TTS API 服务器**：支持语音克隆的 TTS 服务（如基于 OpenAI 的 TTS 服务）
  - 需要支持以下参数：
    - `spk_audio`：说话人参考音频
    - `emo_audio`：情感参考音频
    - `query`：要生成的文本
    - `gpu_id`：GPU ID

### NuGet 包
- Microsoft.Extensions.DependencyInjection 8.0.0
- Microsoft.Extensions.Configuration 8.0.0
- Microsoft.Extensions.Configuration.Json 8.0.0
- Microsoft.Extensions.Configuration.Binder 8.0.0
- System.Text.Json 8.0.0

## 工作流程

VideoTranslate 采用完整的视频翻译工作流程，从视频导入到最终导出，每个步骤都经过精心设计：

### 1. 视频导入阶段
- **导入视频**：从 YouTube 或本地导入视频文件
- **提取音频**：使用 FFmpeg 从视频中提取音频轨道
- **下载字幕**：如果是从 YouTube 导入，自动下载可用字幕
- **创建项目**：生成项目 JSON 文件，记录所有文件路径和元数据

### 2. 语音识别阶段
- **加载音频**：读取源音频文件
- **Whisper 识别**：调用 Whisper.cpp 进行语音识别
- **生成字幕**：将识别结果转换为 SRT 格式字幕
- **保存字幕**：将字幕保存到项目目录

### 3. 字幕翻译阶段
- **解析字幕**：读取 SRT 格式的源字幕
- **批量翻译**：使用选定的翻译 API 批量翻译字幕文本
- **保留时间轴**：翻译时保留原始字幕的时间轴信息
- **保存翻译**：生成目标语言的字幕文件

### 4. TTS 音频生成阶段
- **分段音频**：根据源字幕将源音频分段
- **语音克隆**：使用分段音频作为参考，生成中文 TTS 音频
- **情感控制**：通过 emo_mode 和 emo_weight 控制语音情感
- **保存片段**：将生成的音频片段保存到目标目录

### 5. 音频对齐阶段
- **加载字幕**：读取源字幕和目标字幕
- **时间轴对齐**：根据源字幕时间轴对齐目标音频
- **速度调整**：当中文音频时长与英文不匹配时，自动调整播放速度
- **静音填充**：
  - 前置静音：在音频片段前添加静音以匹配开始时间
  - 后置静音：在音频片段后添加静音以匹配结束时间
- **音频合并**：将所有处理后的音频片段合并为完整音频

### 6. 视频导出阶段
- **音频替换**：使用 FFmpeg 将处理后的音频替换原视频的音频轨道
- **字幕嵌入**：可选地将字幕嵌入到视频中
- **导出文件**：生成最终的翻译视频或音频文件

## 技术架构

### 分层架构

```
┌─────────────────────────────────────┐
│         Program.cs (CLI)            │  命令行接口
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│      Service Layer (服务层)          │
│  ┌──────────────────────────────┐  │
│  │ VideoEditorService          │  │  视频编辑器核心服务
│  │ TrackService                 │  │  轨道管理服务
│  │ YouTubeService               │  │  YouTube 下载服务
│  │ SpeechRecognitionService     │  │  语音识别服务
│  │ TranslationService           │  │  翻译服务
│  │ TTSService                   │  │  TTS 服务
│  │ AudioOverlayService          │  │  音频对齐服务
│  │ SubtitleService              │  │  字幕解析服务
│  │ AudioService                 │  │  音频处理服务
│  │ FFmpegService                │  │  FFmpeg 调用服务
│  │ PathService                  │  │  路径管理服务
│  └──────────────────────────────┘  │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│      Model Layer (模型层)            │
│  ┌──────────────────────────────┐  │
│  │ VideoProject                 │  │  视频项目模型
│  │ VideoEditor                  │  │  视频编辑器模型
│  │ AudioTrack                   │  │  音频轨道模型
│  │ SubtitleTrack                │  │  字幕轨道模型
│  │ AudioSegment                 │  │  音频片段模型
│  │ Subtitle                     │  │  字幕模型
│  │ TTSSegment                   │  │  TTS 片段模型
│  │ YouTubeVideo                 │  │  YouTube 视频模型
│  └──────────────────────────────┘  │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│     Interface Layer (接口层)         │
│  所有服务都通过接口定义，支持依赖注入  │
└─────────────────────────────────────┘
```

### 核心服务说明

#### VideoEditorService
视频编辑器的核心服务，负责：
- 项目的创建、加载和保存
- 从 YouTube 或本地导入视频
- 语音识别和字幕生成
- 视频和音频导出
- 字幕添加到视频

#### TrackService
轨道管理服务，负责：
- 创建和管理音频轨道
- 创建和管理字幕轨道
- 轨道属性的增删改查

#### AudioOverlayService
音频对齐服务，负责：
- 根据字幕时间轴对齐音频
- 自动调整音频速度
- 添加前置和后置静音
- 合并音频片段

#### SpeechRecognitionService
语音识别服务，负责：
- 调用 Whisper.cpp 进行语音识别
- 自动选择可用的 Whisper 模型
- 生成 SRT 格式字幕

#### TTSService
TTS 服务，负责：
- 调用 TTS API 生成音频
- 语音克隆和情感控制
- 断点续传和清理功能

#### TranslationService
翻译服务，负责：
- 支持多种翻译 API
- 批量翻译字幕
- 保留时间轴信息

### 设计模式

- **依赖注入**：使用 Microsoft.Extensions.DependencyInjection 管理服务生命周期
- **接口编程**：所有服务都通过接口定义，便于测试和扩展
- **单一职责**：每个服务只负责一个特定的功能领域
- **配置驱动**：通过配置文件管理外部工具路径和参数

## 代码规范

- 每个文件不超过 200 行
- 使用依赖注入
- 面向接口编程
- 良好的代码风格和命名规范
- 完整的单元测试覆盖

## 项目特点

### 1. 模块化设计
项目采用高度模块化的设计，每个服务都独立封装，通过接口进行交互，便于维护和扩展。

### 2. 智能音频对齐
音频对齐算法能够自动处理中英文时长差异：
- 当中文比英文长时，自动加速中文音频
- 当中文比英文短时，自动添加后置静音
- 支持多级速度调整，确保音质不受影响

### 3. 语音克隆技术
TTS 服务支持语音克隆，能够：
- 保持原说话人的音色特征
- 控制语音情感和语调
- 支持多 GPU 并行处理

### 4. 断点续传
所有长时间运行的任务都支持断点续传：
- TTS 生成：自动跳过已存在的音频片段
- 翻译任务：支持批量翻译，失败可重试
- 项目保存：每次操作后自动保存项目状态

### 5. 测试模式
支持限制处理的字幕数量，用于快速测试：
- 只处理前 N 条字幕，无需处理整个视频
- 适用于翻译、TTS 生成、音频对齐等所有步骤
- 快速验证流程和效果，节省时间和资源

### 6. 灵活的配置
支持多种配置方式：
- 配置文件：管理外部工具路径
- 命令行参数：运行时指定参数
- 环境变量：支持环境变量配置

## 使用建议

### 性能优化

1. **GPU 加速**
   - 使用 GPU 加速 TTS 生成：`--gpu 0`
   - 多 GPU 环境：可以指定不同的 GPU ID

2. **批量处理**
   - 翻译时使用批量大小：`--batch-size 10`
   - 批量大小建议：1-10，根据网络和 API 限制调整

3. **并行处理**
   - 可以同时处理多个项目
   - 每个项目使用不同的 GPU ID

### 质量控制

1. **Whisper 模型选择**
   - large-v3：最高精度，速度较慢
   - large-v2：高精度，速度中等
   - medium：平衡精度和速度
   - small：快速，精度较低

2. **翻译 API 选择**
   - LMStudio：本地大模型，质量高，需要 GPU
   - DeepL：专业翻译，质量高，有 API 限制
   - Google/Bing：快速翻译，质量中等

3. **TTS 参数调整**
   - emo_weight：情感权重，建议 0.7-0.9
   - temperature：生成温度，建议 0.7-0.9
   - top_p：核采样，建议 0.8

### 故障排除

1. **Whisper 识别失败**
   - 检查模型文件是否存在
   - 确认音频格式是否支持
   - 查看错误日志了解详细信息

2. **TTS 生成失败**
   - 检查 TTS 服务是否运行
   - 确认 GPU 内存是否充足
   - 尝试使用 `--clean` 清理旧文件

3. **音频对齐问题**
   - 检查字幕时间轴是否正确
   - 确认音频分段是否完整
   - 查看日志了解对齐过程

## 常见问题

**Q: 如何处理长视频？**
A: 长视频会自动分段处理，每个片段独立生成 TTS 音频，支持断点续传。

**Q: 如何提高翻译质量？**
A: 建议使用 LMStudio 本地大模型翻译，或使用 DeepL 专业翻译 API。

**Q: 音频对齐不准确怎么办？**
A: 检查源字幕时间轴是否准确，确保 Whisper 识别质量，可以尝试使用更大的 Whisper 模型。

**Q: 如何批量处理多个视频？**
A: 可以编写脚本循环调用 VideoTranslate，每个视频使用不同的项目名称。

**Q: TTS 生成的音频音质不好怎么办？**
A: 可以调整 emo_weight 和 temperature 参数，或者使用更高质量的源音频作为参考。

## 许可证

本项目采用 MIT 许可证。

## 贡献

欢迎提交 Issue 和 Pull Request！
