# VideoTranslator - 视频翻译编辑器

## 项目概述

VideoTranslator 是一个基于 Blazor 的视频翻译编辑器，支持视频导入、字幕识别翻译、语音合成和视频导出等功能。

## 核心功能

### 1. 视频导入
- **本地视频导入**: 支持从本地文件系统导入视频文件
- **YouTube 导入**: 支持通过 YouTube URL 导入视频

### 2. 多轨道时间线编辑
- **视频轨道**: 显示视频内容
- **音频轨道**: 管理音频片段
- **字幕轨道**: 管理字幕片段
- **片段操作**: 编辑、复制、分割、删除片段
- **时间线控制**: 播放/暂停、快进/快退、缩放

### 3. 字幕识别和翻译
- **字幕识别**: 自动识别视频中的字幕内容
- **字幕翻译**: 支持多语言字幕翻译（中文、英文等）
- **批量操作**: 批量翻译整条轨道的字幕

### 4. 语音合成
- **TTS 语音生成**: 将字幕转换为语音
- **语音类型选择**: 支持男声/女声
- **自动时长调整**: 根据语音长度自动调整字幕时长

### 5. 音频处理
- **音频长度比较**: 比较原始音频和生成音频的时长
- **音频调整**: 调整音频片段的开始/结束时间
- **音量控制**: 调整音频片段的音量
- **静音控制**: 支持片段静音

### 6. 视频导出
- **视频合成**: 将视频、音频、字幕合成为最终视频
- **导出格式**: 支持常见视频格式

## 技术架构

### 前端技术栈
- **Blazor Server**: 使用 InteractiveServer 渲染模式
- **MudBlazor**: UI 组件库
- **.NET 9.0**: 运行时框架

### 核心组件

#### 数据模型
- `VideoProject`: 视频项目模型
- `Track`: 轨道模型（视频、音频、字幕）
- `Clip`: 片段模型

#### 服务层
- `VideoEditorService`: 编辑器核心服务
  - 项目管理
  - 轨道/片段操作
  - 播放控制
  - 时间管理

#### UI 组件
- `Editor.razor`: 主编辑器页面
- `TimelineComponent.razor`: 时间线组件
- `ClipEditDialog.razor`: 片段编辑对话框
- `BatchOperationsDialog.razor`: 批量操作对话框
- `ClipContextMenu.razor`: 片段右键菜单

### 状态管理
- 使用 Blazor 的状态管理机制
- 通过 `VideoEditorService` 集中管理项目状态
- 事件通知机制（`OnTimeChanged` 等）

## 用户界面

### 主界面布局
- **顶部工具栏**: 导入/导出按钮、批量操作
- **视频播放器**: 显示视频内容，支持播放控制
- **时间线**: 多轨道可视化编辑区域
- **缩放控制**: 时间线缩放功能

### 片段操作
- **编辑**: 修改片段属性（名称、时间、内容、音量等）
- **复制**: 复制片段到新位置
- **分割**: 在指定位置分割片段
- **删除**: 删除片段
- **生成语音**: 为字幕生成语音
- **翻译**: 翻译字幕内容
- **识别字幕**: 识别视频中的字幕

## 开发进度

### 已完成
- ✅ 基础数据模型（Track、Clip、VideoProject）
- ✅ VideoEditorService 核心服务
- ✅ 主编辑器界面（Editor.razor）
- ✅ 时间线组件（TimelineComponent.razor）
- ✅ 片段编辑对话框（ClipEditDialog.razor）
- ✅ 批量操作对话框（BatchOperationsDialog.razor）
- ✅ 片段右键菜单（ClipContextMenu.razor）
- ✅ 基础样式（editor.css）

### 待实现
- ⏳ 视频导入功能（YouTube、本地）
- ⏳ 字幕识别功能
- ⏳ 字幕翻译功能
- ⏳ 语音合成功能
- ⏳ 音频长度比较和调整功能
- ⏳ 视频合成和导出功能

## 数据库配置

MySQL 位置: `C:\Program Files\MySQL\MySQL Workbench 8.0\mysql.exe`

## 项目结构

```
VideoTranslator/
├── Components/
│   ├── Pages/
│   │   └── Editor.razor          # 主编辑器页面
│   └── Shared/
│       ├── TimelineComponent.razor      # 时间线组件
│       ├── ClipEditDialog.razor         # 片段编辑对话框
│       ├── BatchOperationsDialog.razor  # 批量操作对话框
│       └── ClipContextMenu.razor       # 片段右键菜单
├── Models/
│   ├── Track.cs                  # 轨道模型
│   └── VideoProject.cs           # 项目模型
├── Services/
│   └── VideoEditorService.cs      # 编辑器服务
└── wwwroot/
    └── css/
        └── editor.css            # 编辑器样式
```

## 编译和运行

### 编译项目
```bash
dotnet build
```

### 运行项目
```bash
dotnet run
```

## 注意事项

- 当前项目使用 MudBlazor 的旧版 Dialog API，建议后续迁移到 `ShowAsync` 方法
- 部分 MudBlazor 组件属性使用小写命名，会触发警告，但不影响功能
- 未使用的字段（如 `videoElement`）需要清理

## 未来规划

1. 实现视频导入功能（支持多种格式）
2. 集成字幕识别 API（如 Whisper）
3. 集成翻译 API（如 Google Translate、DeepL）
4. 集成 TTS 语音合成 API
5. 实现视频合成和导出功能
6. 添加项目保存/加载功能
7. 优化性能和用户体验
