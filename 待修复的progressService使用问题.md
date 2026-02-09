# progressService 使用问题清单

> 本文档列出项目中所有 `progressService` 使用不当的地方，需要修正为正确的方法。

## 使用原则

| 场景 | 方法 | 说明 |
|------|------|------|
| 真正的错误 | `Error()` | 操作失败，需要用户关注 |
| 可恢复/警告 | `Warning()` | 跳过某些步骤，但不是致命错误 |
| 正常进度 | `Report()` | 正常的处理过程 |

---

## 问题清单

### 1. IYouTubeService.cs (多个问题)

#### ✅ 已修复
- 行 402: `progress?.Error(...)` - 正确

#### ❌ 需要修复
- 行 82: `progress?.Report("✗ 获取字幕列表失败...")` → 改为 `Error()`
- 行 99: `progress?.Report("✗ 获取流清单失败...")` → 改为 `Error()`
- 行 126: `progress?.Report("✗ 方法1失败...")` → 改为 `Error()`
- 行 153: `progress?.Report("✗ 方法2失败...")` → 改为 `Error()`
- 行 188: `progress?.Report("✗ 获取音频格式列表失败...")` → 改为 `Error()`
- 行 220: `progress?.Report("✗ 获取视频格式列表失败...")` → 改为 `Error()`
- 行 388: `progress?.Report("第一次合并失败，尝试使用转码合并...")` → 改为 `Warning()` (尝试恢复策略)
- 行 434: `progress?.Report("下载失败: {ex.Message}")` → 改为 `Error()`
- 行 601: `progress?.Report("下载失败: {ex.Message}")` → 改为 `Error()`
- 行 691: `progress?.Report("视频流匹配失败，回退到最大视频流")` → 改为 `Warning()` (回退策略)
- 行 697: `progress?.Report("音频流匹配失败，回退到最高码率音频流")` → 改为 `Warning()` (回退策略)
- 行 792: `progress?.Report("第一次合并失败，尝试使用转码合并...")` → 改为 `Warning()` (尝试恢复策略)
- 行 806: `progress?.Report("错误: 文件不存在！列出目录内容:")` → 改为 `Error()`
- 行 838: `progress?.Report("下载失败: {ex.Message}")` → 改为 `Error()`
- 行 960: `progress?.Report("字幕下载失败: {ex.Message}")` → 改为 `Error()`

---

### 2. PurfviewFasterWhisperRecognitionService.cs

#### ❌ 需要修复
- 行 91: `progress?.Report("删除临时文件失败: {ex.Message}")` → 改为 `Warning()`
- 行 219: `progress?.Report("删除临时文件失败: {ex.Message}")` → 改为 `Warning()`
- 行 278: `progress?.Report("[音频预处理] 音频转换失败: {ex.Message}")` → 改为 `Error()`
- 行 349: `progress?.Report("[MakeWavePeaks] 生成波形数据失败: {ex.Message}")` → 改为 `Warning()` (非致命)
- 行 417: `progress?.Report("删除临时JSON文件失败: {ex.Message}")` → 改为 `Warning()`

#### ✅ 已修复
- 行 185: `progress?.Warning("波形数据生成失败，跳过时间轴调整")` - 正确

---

### 3. WhisperRecognitionService.cs

#### ❌ 需要修复
- 行 152: `progress?.Report("删除临时文件失败: {ex.Message}")` → 改为 `Warning()`
- 行 257: `progress?.Report("删除临时文件失败: {ex.Message}")` → 改为 `Warning()`
- 行 513: `progress?.Report("[音频预处理] 音频转换失败: {ex.Message}")` → 改为 `Error()`
- 行 619: `progress?.Report("[MakeWavePeaks] 生成波形数据失败: {ex.Message}")` → 改为 `Warning()` (非致命)

#### ✅ 已修复
- 行 104: `progress?.Warning("删除临时文件失败: {ex.Message}")` - 正确
- 行 223: `progress?.Warning("波形数据生成失败，跳过时间轴调整")` - 正确

---

### 4. LMStudioTranslationService.cs

#### ❌ 需要修复
- 行 157: `progress?.Report("批次 {batchIndex} 完全失败，使用原文")` → 改为 `Warning()`
- 行 179: `progress?.Report("翻译失败: {failedCount}")` → 改为 `Warning()`
- 行 227: `progress?.Report("提示LM: 上次返回{actual}条，需要{expected}条")` → 改为 `Warning()`
- 行 300: `progress?.Report("✗ 错误: 批次{batchIndex}翻译数量不匹配")` → 改为 `Error()` (严重错误)
- 行 317: `progress?.Report("✗ 批次{batchIndex}连续{maxRetries}次失败，使用部分翻译结果")` → 改为 `Warning()`

#### ✅ 已修复
- 行 243: `progress?.Error("错误: 翻译结果为空")` - 正确
- 行 341: `progress?.Report("✗ 错误: {ex.Message}")` → 已改为 `Error()`

---

### 5. ChatService.cs

#### ✅ 已修复
- 行 46: `progress?.Error("错误: {ex.Message}")` - 正确

---

### 6. VideoVadWorkflowService.cs

#### ❌ 需要修复
- 行 65: `progress?.Report("[VideoVadWorkflowService] 获取音频时长失败: {ex.Message}")` → 改为 `Error()`
- 行 145: `progress?.Report("[VideoVadWorkflowService] 片段 {i + 1} 分割失败...")` → 改为 `Error()`
- 行 148: `progress?.Report("[VideoVadWorkflowService] FFmpeg错误: {stdError}")` → 改为 `Error()`
- 行 187: `progress?.Report("[VideoVadWorkflowService] 最后一个片段分割失败...")` → 改为 `Error()`
- 行 190: `progress?.Report("[VideoVadWorkflowService] FFmpeg错误: {lastStdError}")` → 改为 `Error()`
- 行 237: `progress?.Report("[VideoVadWorkflowService] 检查音频流失败: {ex.Message}")` → 改为 `Error()`
- 行 302: `progress?.Report("[VideoVadWorkflowService] 音频提取失败: {ex.Message}")` → 改为 `Error()`
- 行 354: `progress?.Report("[VideoVadWorkflowService] 音频转换失败: {ex.Message}")` → 改为 `Error()`
- 行 488: `progress?.Report("[VideoVadWorkflowService] 片段 {segment.Index} 分割失败...")` → 改为 `Error()`
- 行 592: `progress?.Report("[VideoVadWorkflowService] VAD工作流执行失败（无音频流）...")` → 改为 `Error()`
- 行 656: `progress?.Report("[VideoVadWorkflowService] 完整VAD工作流执行失败: {ex.Message}")` → 改为 `Error()`
- 行 739: `progress?.Report("[VideoVadWorkflowService] VAD工作流执行失败: {ex.Message}")` → 改为 `Error()`

---

### 7. SRTTrackInfo.cs

#### ✅ 已修复
- 行 279: `s.progress?.Error("触发 OnTrackChanged 事件时发生错误: {ex.Message}")` - 正确
- 行 365: `s.progress?.Error("触发 OnTrackChanged 事件时发生错误: {ex.Message}")` - 正确
- 行 374: `s.progress?.Error("处理TTS片段 {segment?.Index} 时发生错误: {ex.Message}")` - 正确

---

### 8. ITranslationService.cs

#### ❌ 需要修复
- 行 85: `progress?.Report("✗ 翻译失败，保留原文")` → 改为 `Warning()`
- 行 136: `progress?.Report("批量翻译失败: {ex.Message}")` → 改为 `Error()`
- 行 155: `progress?.Report("批量翻译失败: {ex.Message}{ex.InnerException?.Message}")` → 改为 `Error()`
- 行 223: `progress?.Report("Google批量翻译失败: HTTP {response.StatusCode} - {errorContent}")` → 改为 `Error()`

---

### 9. VoskRecognitionService.cs

#### ❌ 需要修复
- 行 101: `progress?.Report("删除临时文件失败: {ex.Message}")` → 改为 `Warning()`
- 行 159: `progress?.Report("[音频预处理] 音频转换失败: {ex.Message}")` → 改为 `Error()`
- 行 230: `progress?.Report("说话人识别模型加载失败: {ex.Message}")` → 改为 `Error()`

---

### 10. TTSServiceManager.cs

#### ❌ 需要修复
- 行 58: `progress?.Report("  ✗ 服务启动失败")` → 改为 `Error()`
- 行 179: `progress?.Report("[警告] GPU检测失败: {ex.Message}")` → 改为 `Warning()`
- 行 196: `progress?.Report("[警告] 端口检测失败: {ex.Message}")` → 改为 `Warning()`
- 行 240: `progress?.Report("  错误: api.py 不存在: {apiPyPath}")` → 改为 `Error()`
- 行 246: `progress?.Report("  错误: uv.exe 不存在: {uvPath}")` → 改为 `Error()`
- 行 279: `progress?.Report("  错误: 无法启动进程")` → 改为 `Error()`
- 行 327: `progress?.Report("  [错误] {line}")` → 改为 `Error()`
- 行 357: `progress?.Report("  错误: {ex.Message}")` → 改为 `Error()`
- 行 399: `progress?.Report("  停止服务失败: {ex.Message}")` → 改为 `Error()` 或 `Warning()` (取决于严重程度)

---

### 11. SherpaSpeakerDiarizationService.cs

#### ❌ 需要修复
- 行 116: `progress?.Report("删除临时文件失败: {ex.Message}")` → 改为 `Warning()`
- 行 197: `progress?.Report("[音频预处理] 音频转换失败: {ex.Message}")` → 改为 `Error()`

---

### 12. ITTSService.cs

#### ❌ 需要修复
- 行 99: `progress?.Report("  服务 [{urlIndex + 1}] 失败: {response.StatusCode}...")` → 改为 `Error()`
- 行 113: `progress?.Report("  服务 [{urlIndex + 1}] 错误: {ex.Message}")` → 改为 `Error()`
- 行 164: `progress?.Report("失败: {Path.GetFileName(command.OutputAudio)}")` → 改为 `Error()`
- 行 296: `progress?.Report("  失败: {Path.GetFileName(currentCommand.OutputAudio)}")` → 改为 `Error()`
- 行 340: `progress?.Report("  生成失败: {failedCount}")` → 改为 `Error()`

---

### 13. ISpeechRecognitionService.cs

#### ❌ 需要修复
- 行 303: `progress?.Report("[ERROR] SRT生成失败: {ex.Message}")` → 改为 `Error()`

---

### 14. VideoProject.cs

#### ❌ 需要修复
- 行 121: `progress?.Report("[Warning] Failed to delete original file: {originalFilePath}, Error: {ex.Message}")` → 改为 `Warning()`

---

### 15. AsyncSimpleAction.cs

#### ✅ 已修复
- 行 78: `progress?.Error(exception?.Message ?? "任务执行失败")` - 正确

---

## 总结

### 统计

| 状态 | 数量 |
|------|------|
| ✅ 已修复 | 10 处 |
| ❌ 需要修复 | 约 80+ 处 |

### 分类统计

| 方法 | 需要修复的数量 |
|------|---------------|
| `Report()` → `Error()` | ~50 处 |
| `Report()` → `Warning()` | ~30 处 |

### 文件统计

| 文件 | 问题数量 |
|------|----------|
| IYouTubeService.cs | ~15 处 |
| VideoVadWorkflowService.cs | ~12 处 |
| TTSServiceManager.cs | ~9 处 |
| LMStudioTranslationService.cs | ~6 处 |
| ITTSService.cs | ~5 处 |
| WhisperRecognitionService.cs | ~4 处 |
| PurfviewFasterWhisperRecognitionService.cs | ~5 处 |
| VoskRecognitionService.cs | ~3 处 |
| SherpaSpeakerDiarizationService.cs | ~2 处 |
| ITranslationService.cs | ~4 处 |
| ISpeechRecognitionService.cs | ~1 处 |
| VideoProject.cs | ~1 处 |

---

## 修复优先级

### 高优先级 (Error)
- 下载失败
- 翻译失败
- 音频提取/转换失败
- 核心功能失败

### 中优先级 (Warning)
- 临时文件删除失败
- 回退策略触发
- 尝试恢复的步骤
- 非致命错误

---

*生成时间: 2026-02-06*
