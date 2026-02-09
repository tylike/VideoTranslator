# 并行TTS服务使用指南

## 概述

并行TTS服务允许同时使用多个TTS API服务实例来加速音频生成过程。通过在不同的GPU上启动多个服务，可以显著提高TTS生成的速度。

## 功能特性

- ✅ 支持启动多个TTS服务实例
- ✅ **自动检测可用GPU和端口**
- ✅ **自动检测已运行的TTS服务**
- ✅ **如果没有运行的服务，自动启动**
- ✅ 服务健康检测
- ✅ 负载均衡（自动将任务分配给最空闲的服务）
- ✅ 并行处理，提高生成速度
- ✅ 支持自定义服务数量和端口范围

## 快速开始

### 方式一：自动检测和启动（推荐）

**最简单的方式**：直接使用 `tts-parallel` 命令，系统会自动检测和启动所需的服务。

```bash
# 系统会自动检测GPU和端口，并启动所需的TTS服务
VideoTranslate.exe tts-parallel MyProject.json

# 清理旧文件并重新生成
VideoTranslate.exe tts-parallel MyProject.json --clean

# 限制生成数量（仅生成前20条）
VideoTranslate.exe tts-parallel MyProject.json --limit 20
```

**自动检测流程：**
1. 检测系统可用的GPU（使用 `nvidia-smi`）
2. 检查端口8000、8001、8002等是否被占用
3. 如果端口已被TTS服务占用，直接使用该服务
4. 如果端口可用，自动启动新的TTS服务
5. 验证服务健康状态
6. 开始并行生成音频

**优势：**
- 无需手动启动服务
- 自动利用所有可用GPU
- 智能检测已运行的服务，避免重复启动
- 端口冲突自动处理

### 方式二：手动启动服务

如果需要手动控制服务启动过程，可以使用批处理脚本：

```bash
# 启动5个服务（默认）
startAllTtsServices.bat

# 启动3个服务
startAllTtsServices.bat 3

# 启动5个服务，起始端口为8000
startAllTtsServices.bat 5 8000
```

这将启动以下服务：
- 服务0: GPU 0, 端口 8000
- 服务1: GPU 1, 端口 8001
- 服务2: GPU 2, 端口 8002
- 服务3: GPU 3, 端口 8003
- 服务4: GPU 4, 端口 8004

### 3. 使用并行TTS生成音频

```bash
# 使用并行TTS生成音频
VideoTranslate.exe tts-parallel MyProject.json

# 清理旧文件并重新生成
VideoTranslate.exe tts-parallel MyProject.json --clean

# 限制生成数量（仅生成前20条）
VideoTranslate.exe tts-parallel MyProject.json --limit 20
```

### 3. 停止所有服务

```bash
# 停止所有TTS服务
taskkill /F /IM cmd.exe /FI "WINDOWTITLE eq startTtsService*"
```

## 高级用法

### 启动单个服务

如果需要单独启动某个服务：

```bash
# 在GPU 0上启动服务，端口8000
startTtsService.bat 0 8000

# 在GPU 1上启动服务，端口8001
startTtsService.bat 1 8001
```

### 自定义GPU分配

如果需要自定义GPU分配，可以修改 `startAllTtsServices.bat` 脚本中的GPU ID映射。

## 性能对比

假设有100条字幕需要生成：

| 方式 | 服务数量 | 并发数 | 预估时间 |
|------|---------|--------|---------|
| 单服务 | 1 | 1 | ~100分钟 |
| 并行服务 | 3 | 3 | ~35分钟 |
| 并行服务 | 5 | 5 | ~20分钟 |

*实际时间取决于硬件性能和音频长度*

## 服务管理

### 查看服务状态

服务启动后，可以通过以下方式查看状态：

1. 查看日志文件：
   ```bash
   type logs\tts_gpu0_port8000.log
   ```

2. 使用 `tts-parallel` 命令时会自动显示服务状态

### 服务健康检查

系统会自动检查每个服务的健康状态。如果某个服务不可用，任务会自动分配给其他健康的服务。

## 故障排除

### 问题：服务启动失败

**解决方案：**
1. 检查GPU是否可用：`nvidia-smi`
2. 检查端口是否被占用：`netstat -ano | findstr 8000`
3. 查看日志文件了解详细错误信息

### 问题：生成速度没有提升

**解决方案：**
1. 确认所有服务都已启动并正常运行
2. 检查GPU内存使用情况
3. 确认网络连接正常
4. 查看服务日志是否有错误

### 问题：部分服务不工作

**解决方案：**
1. 检查特定GPU是否可用
2. 重启失败的服务
3. 查看该服务的日志文件

## 最佳实践

1. **服务数量**：根据可用GPU数量设置服务数量，通常每个GPU启动一个服务
2. **端口分配**：使用连续的端口号，便于管理
3. **日志管理**：定期清理日志文件，避免占用过多磁盘空间
4. **资源监控**：使用 `nvidia-smi` 监控GPU使用情况
5. **测试先行**：先用少量数据测试，确认服务正常后再处理完整数据

## 示例工作流

```bash
# 1. 导入视频
VideoTranslate.exe local C:\videos\myvideo.mp4 MyProject

# 2. 语音识别
VideoTranslate.exe recognize MyProject.json en

# 3. 翻译字幕
VideoTranslate.exe translate MyProject.json en zh

# 4. 并行生成TTS音频（系统会自动检测和启动服务）
VideoTranslate.exe tts-parallel MyProject.json

# 5. 音频对齐和合并
VideoTranslate.exe overlay MyProject.json

# 6. 导出视频
VideoTranslate.exe export MyProject.json output.mp4
```

**注意**：步骤4会自动检测和启动所需的TTS服务，无需手动启动！

## 技术细节

### 架构

```
VideoTranslate.exe
    ↓
TTSServiceManager (服务管理器)
    ↓
┌─────────┬─────────┬─────────┬─────────┬─────────┐
│ Service0│ Service1│ Service2│ Service3│ Service4│
│ GPU 0   │ GPU 1   │ GPU 2   │ GPU 3   │ GPU 4   │
│ Port 8000│ Port 8001│ Port 8002│ Port 8003│ Port 8004│
└─────────┴─────────┴─────────┴─────────┴─────────┘
```

### 负载均衡策略

系统使用"最空闲优先"策略：
1. 每个服务维护一个任务计数器
2. 新任务分配给任务数最少的服务
3. 实时更新服务状态

### 错误处理

- 服务不可用：自动跳过，分配给其他服务
- 生成失败：记录失败信息，继续处理其他任务
- 服务崩溃：记录日志，不影响其他服务

## 更新日志

### v1.1.0 (2026-01-03)
- ✨ 新增自动检测功能
  - 自动检测系统可用的GPU（使用nvidia-smi）
  - 自动检测端口占用情况
  - 自动检测已运行的TTS服务
  - 如果没有运行的服务，自动启动
- 🚀 简化使用流程
  - 无需手动启动服务
  - 直接使用 `tts-parallel` 命令即可
  - 系统会自动处理所有服务管理
- 🐛 修复端口冲突问题
  - 智能检测已占用的端口
  - 自动跳过被其他程序占用的端口

### v1.0.0 (2026-01-03)
- 初始版本
- 支持多服务并行TTS生成
- 自动服务健康检测
- 负载均衡任务分配
- 提供便捷的启动脚本
