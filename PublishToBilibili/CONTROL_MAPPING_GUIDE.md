# 控件定位与映射系统使用指南

## 概述

本系统提供了一套完整的控件定位和映射解决方案，用于解决UI自动化中控件定位不可靠的问题。系统支持不同场景（自制/转载）下的控件定位，并能自动分析日志生成控件定位策略。

## 核心组件

### 1. 模型类

#### PublishScenario.cs
定义了发布场景枚举：
- `SelfMade` - 自制视频模式
- `Repost` - 转载视频模式

#### ControlLocator.cs
控件定位器模型，包含以下属性：
- `FieldType` - 字段类型（标题、类型、标签等）
- `ControlType` - UI控件类型
- `NameContains` - 名称包含的文本
- `AutomationId` - 自动化ID
- `MinX/MaxX/MinY/MaxY` - 位置范围
- `Index` - 控件索引
- `MustBeEnabled` - 是否必须启用
- `MustBeVisible` - 是否必须可见

### 2. 服务类

#### ControlLocatorStrategy.cs
控件定位策略服务，提供：
- 基于场景的控件定位器管理
- 控件匹配逻辑
- 定位器更新功能

#### ControlMappingRecorder.cs
控件映射记录器，提供：
- 记录窗口所有控件信息
- 保存控件映射到文件
- 分析控件稳定性
- 生成定位器报告

#### LogAnalyzer.cs
日志分析工具，提供：
- 分析现有UI元素日志
- 比较不同场景的控件差异
- 生成控件定位建议
- 生成C#代码形式的定位器

#### PublishFormModelV2.cs
基于策略的表单模型，提供：
- 场景感知的控件定位
- 统一的控件访问接口
- 控件位置日志功能

#### FormValidationServiceV2.cs
基于新定位策略的验证服务，提供：
- 场景感知的表单验证
- 使用PublishFormModelV2进行控件定位
- 详细的验证报告

### 3. 工具类

#### ControlMappingTool.cs
控件映射工具，提供：
- 一键生成完整报告
- 记录当前窗口控件
- 测试定位策略
- 分析现有日志

## 使用方法

### 方法1: 分析现有日志

```csharp
using PublishToBilibili.Services;

var logAnalyzer = new LogAnalyzer();

// 分析所有日志
logAnalyzer.AnalyzeAllLogs();

// 分析特定场景
logAnalyzer.AnalyzeSpecificScenario(PublishScenario.SelfMade);

// 比较场景差异
logAnalyzer.CompareScenarios();
```

### 方法2: 记录当前窗口控件

```csharp
using PublishToBilibili.Services;
using PublishToBilibili.Models;

var recorder = new ControlMappingRecorder();

// 记录窗口控件
recorder.RecordWindowControls(
    windowHandle,
    PublishScenario.SelfMade,
    "Initial_State"
);
```

### 方法3: 使用新的表单模型

```csharp
using PublishToBilibili.Services;
using PublishToBilibili.Models;

var automation = new UIA3Automation();
var window = automation.FromHandle(windowHandle);

// 创建模型（指定场景）
var model = new PublishFormModelV2(window, PublishScenario.SelfMade);

// 访问控件（自动使用正确的定位策略）
var titleEditBox = model.TitleEditBox;
var tagsEditBox = model.TagsEditBox;

// 切换场景
model.SetScenario(PublishScenario.Repost);

// 记录控件位置（用于调试）
model.LogElementPositions();
```

### 方法4: 使用新的验证服务

```csharp
using PublishToBilibili.Services;
using PublishToBilibili.Models;

var validationService = new FormValidationServiceV2();

var publishInfo = new PublishInfo
{
    Title = "测试标题",
    Type = "自制",
    Tags = new List<string> { "科技", "数码" },
    Description = "测试描述",
    IsRepost = false,
    EnableOriginalWatermark = true,
    EnableNoRepost = true
};

var result = validationService.ValidatePublishForm(windowHandle, publishInfo);
```

### 方法5: 使用完整工具

```csharp
using PublishToBilibili.Tools;

var mappingTool = new ControlMappingTool();

// 生成完整报告（分析日志、比较场景、生成定位建议）
mappingTool.GenerateFullReport();

// 记录当前窗口
mappingTool.RecordCurrentWindow(windowHandle, PublishScenario.SelfMade, "Stage1");

// 测试定位策略
mappingTool.TestLocatorStrategy(windowHandle, PublishScenario.SelfMade);
```

## 工作流程

### 推荐的工作流程：

1. **收集日志数据**
   - 运行现有的表单填写程序，生成UI元素日志
   - 确保覆盖自制和转载两种场景

2. **分析日志**
   ```csharp
   var logAnalyzer = new LogAnalyzer();
   logAnalyzer.AnalyzeAllLogs();
   logAnalyzer.CompareScenarios();
   ```

3. **查看生成的报告**
   - 检查 `ControlMappings` 目录
   - 查看 `LocatorSuggestions_*.txt` 文件
   - 了解不同场景下的控件差异

4. **更新定位策略**
   - 根据报告中的建议更新 `ControlLocatorStrategy.cs`
   - 调整定位器的位置范围和匹配条件

5. **测试定位策略**
   ```csharp
   var model = new PublishFormModelV2(window, PublishScenario.SelfMade);
   model.LogElementPositions();
   ```

6. **集成到生产代码**
   - 使用 `PublishFormModelV2` 替换旧的 `PublishFormModel`
   - 使用 `FormValidationServiceV2` 替换旧的 `FormValidationService`

## 输出文件说明

### 控件映射文件
- 格式：`Controls_{Scenario}_{Stage}_{Timestamp}.txt`
- 内容：CSV格式的控件信息
- 位置：`bin/Debug/net10.0-windows/ControlMappings/`

### 定位器报告
- 格式：`LocatorReport_{Scenario}_{Timestamp}.txt`
- 内容：详细的控件定位建议
- 位置：`bin/Debug/net10.0-windows/ControlMappings/`

### 定位器建议
- 格式：`LocatorSuggestions_{Scenario}_{Timestamp}.txt`
- 内容：C#代码形式的定位器定义
- 位置：`bin/Debug/net10.0-windows/ControlMappings/`

## 关键优势

1. **场景感知**：自动识别自制/转载模式，使用不同的定位策略
2. **多特征匹配**：不只依赖索引，还使用位置、名称、AutomationId等多个特征
3. **自动分析**：通过日志分析自动生成定位建议
4. **易于维护**：定位策略集中管理，便于更新和调试
5. **向后兼容**：保留了原有的 `PublishFormModel`，可以逐步迁移

## 注意事项

1. 定位器的位置范围（MinY/MaxY）可能需要根据实际屏幕分辨率调整
2. 对于动态生成的控件（如标签按钮），需要特殊处理
3. 建议定期运行日志分析，确保定位策略的准确性
4. 在不同分辨率下测试定位策略的稳定性

## 示例程序

运行示例程序查看完整演示：

```csharp
using PublishToBilibili.Examples;

ControlMappingExample.RunExample();
```

## 故障排除

### 控件找不到
- 检查场景设置是否正确
- 运行 `LogElementPositions()` 查看实际控件位置
- 更新定位器的位置范围

### 验证失败
- 确认使用的是 `FormValidationServiceV2`
- 检查 `PublishInfo` 的 `IsRepost` 属性是否正确
- 查看详细的验证日志

### 日志分析无结果
- 确认日志文件路径正确
- 检查日志文件格式是否符合预期
- 确保日志包含足够的控件信息
