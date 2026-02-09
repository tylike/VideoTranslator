# 控件定位与映射系统

## 问题描述

在UI自动化测试中，控件定位面临以下挑战：

1. **控件顺序不稳定**：在不同场景（自制/转载）下，控件顺序可能不同
2. **验证控件与输入控件不同**：如标签输入后会创建N个按钮控件
3. **界面动态变化**：选择不同类型后，界面布局和控件位置会变化
4. **定位方式单一**：仅依赖索引或单一特征定位不够可靠

## 解决方案

本系统提供了一套完整的控件定位和映射解决方案，包括：

### 核心功能

1. **场景感知的控件定位**：区分自制/转载模式，使用不同的定位策略
2. **多特征匹配**：基于控件类型、位置、名称、AutomationId等多个特征进行匹配
3. **日志自动分析**：分析现有UI元素日志，自动生成控件定位建议
4. **控件映射记录**：记录不同场景下的控件特征，便于分析
5. **策略化管理**：集中管理控件定位策略，便于维护和更新

### 项目结构

```
PublishToBilibili/
├── Models/
│   ├── PublishInfo.cs              # 发布信息模型
│   ├── PublishScenario.cs          # 场景枚举（新增）
│   └── ControlLocator.cs           # 控件定位器模型（新增）
├── Services/
│   ├── PublishFormModel.cs         # 原始表单模型（保留）
│   ├── PublishFormModelV2.cs       # 新表单模型（新增）
│   ├── FormValidationService.cs    # 原始验证服务（保留）
│   ├── FormValidationServiceV2.cs  # 新验证服务（新增）
│   ├── ControlLocatorStrategy.cs   # 控件定位策略（新增）
│   ├── ControlMappingRecorder.cs   # 控件映射记录器（新增）
│   └── LogAnalyzer.cs              # 日志分析工具（新增）
├── Tools/
│   └── ControlMappingTool.cs       # 控件映射工具（新增）
├── Examples/
│   └── ControlMappingExample.cs    # 使用示例（新增）
├── Program.cs                       # 原始程序入口
├── ProgramV2.cs                     # 新程序入口（新增）
└── CONTROL_MAPPING_GUIDE.md         # 详细使用指南（新增）
```

## 快速开始

### 1. 分析现有日志

```bash
# 运行程序并选择选项1
dotnet run
```

或使用代码：

```csharp
var logAnalyzer = new LogAnalyzer();
logAnalyzer.AnalyzeAllLogs();
```

### 2. 比较场景差异

```bash
# 运行程序并选择选项2
dotnet run
```

或使用代码：

```csharp
var logAnalyzer = new LogAnalyzer();
logAnalyzer.CompareScenarios();
```

### 3. 生成完整报告

```bash
# 运行程序并选择选项3
dotnet run
```

或使用代码：

```csharp
var mappingTool = new ControlMappingTool();
mappingTool.GenerateFullReport();
```

### 4. 测试定位策略

```bash
# 运行程序并选择选项4
dotnet run
```

### 5. 记录当前窗口控件

```bash
# 运行程序并选择选项5
dotnet run
```

## 使用新模型

### 替换旧的表单模型

```csharp
// 旧方式
var oldModel = new PublishFormModel(window);
var titleEditBox = oldModel.TitleEditBox;

// 新方式（推荐）
var newModel = new PublishFormModelV2(window, PublishScenario.SelfMade);
var titleEditBox = newModel.TitleEditBox;

// 切换场景
newModel.SetScenario(PublishScenario.Repost);
```

### 替换旧的验证服务

```csharp
// 旧方式
var oldValidation = new FormValidationService();
var result = oldValidation.ValidatePublishForm(windowHandle, publishInfo);

// 新方式（推荐）
var newValidation = new FormValidationServiceV2();
var result = newValidation.ValidatePublishForm(windowHandle, publishInfo);
```

## 工作流程

### 推荐的完整工作流程：

1. **收集日志数据**
   - 运行现有的表单填写程序
   - 确保覆盖自制和转载两种场景
   - 生成UI元素日志到 `DebugLogs` 目录

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

## 输出文件

### 控件映射文件
- 位置：`bin/Debug/net10.0-windows/ControlMappings/`
- 格式：`Controls_{Scenario}_{Stage}_{Timestamp}.txt`
- 内容：CSV格式的控件信息

### 定位器报告
- 位置：`bin/Debug/net10.0-windows/ControlMappings/`
- 格式：`LocatorReport_{Scenario}_{Timestamp}.txt`
- 内容：详细的控件定位建议

### 定位器建议
- 位置：`bin/Debug/net10.0-windows/ControlMappings/`
- 格式：`LocatorSuggestions_{Scenario}_{Timestamp}.txt`
- 内容：C#代码形式的定位器定义

## 关键优势

1. **场景感知**：自动识别自制/转载模式，使用不同的定位策略
2. **多特征匹配**：不只依赖索引，还使用位置、名称、AutomationId等多个特征
3. **自动分析**：通过日志分析自动生成定位建议
4. **易于维护**：定位策略集中管理，便于更新和调试
5. **向后兼容**：保留了原有的 `PublishFormModel`，可以逐步迁移
6. **可扩展性**：支持添加新的场景和字段类型

## 技术细节

### 控件定位策略

定位器使用以下特征进行匹配：

1. **控件类型**（ControlType）：Edit、CheckBox、Button等
2. **名称**（NameContains）：控件显示的文本
3. **自动化ID**（AutomationId）：控件的唯一标识
4. **位置范围**（MinX/MaxX/MinY/MaxY）：控件的坐标范围
5. **状态**（IsEnabled/IsOffscreen）：控件的启用和可见状态

### 场景管理

系统支持两种主要场景：

- **SelfMade**：自制视频模式
  - 标题位置：Y ≈ -30 到 10
  - 类型位置：Y ≈ 60 到 80
  - 标签位置：Y ≈ 150 到 170
  - 描述位置：Y ≈ 400 到 430

- **Repost**：转载视频模式
  - 标题位置：Y ≈ 190 到 210
  - 类型位置：Y ≈ 280 到 300
  - 标签位置：Y ≈ 340 到 360
  - 描述位置：Y ≈ 620 到 640
  - 来源地址：Y ≈ 305 到 315

## 故障排除

### 控件找不到

**症状**：`FindElement` 返回 null

**解决方案**：
1. 检查场景设置是否正确
2. 运行 `LogElementPositions()` 查看实际控件位置
3. 更新定位器的位置范围
4. 检查控件名称是否匹配

### 验证失败

**症状**：验证结果显示 FAIL

**解决方案**：
1. 确认使用的是 `FormValidationServiceV2`
2. 检查 `PublishInfo` 的 `IsRepost` 属性是否正确
3. 查看详细的验证日志
4. 确认控件定位策略是否正确

### 日志分析无结果

**症状**：分析后没有生成报告

**解决方案**：
1. 确认日志文件路径正确
2. 检查日志文件格式是否符合预期
3. 确保日志包含足够的控件信息
4. 检查是否有足够的日志文件用于分析

## 示例代码

### 完整示例

```csharp
using System;
using FlaUI.UIA3;
using PublishToBilibili.Models;
using PublishToBilibili.Services;
using PublishToBilibili.Tools;

class Program
{
    static void Main(string[] args)
    {
        // 1. 生成完整报告
        var mappingTool = new ControlMappingTool();
        mappingTool.GenerateFullReport();

        // 2. 使用新模型
        var automation = new UIA3Automation();
        var window = automation.FromHandle(windowHandle);
        
        var model = new PublishFormModelV2(window, PublishScenario.SelfMade);
        var titleEditBox = model.TitleEditBox;
        
        // 3. 使用新验证服务
        var validationService = new FormValidationServiceV2();
        var publishInfo = new PublishInfo
        {
            Title = "测试标题",
            Type = "自制",
            Tags = new List<string> { "科技", "数码" },
            Description = "测试描述",
            IsRepost = false
        };
        
        var result = validationService.ValidatePublishForm(windowHandle, publishInfo);
    }
}
```

## 贡献指南

1. 添加新的场景：在 `PublishScenario.cs` 中添加枚举值
2. 添加新的字段类型：在 `FormFieldType` 中添加枚举值
3. 更新定位策略：在 `ControlLocatorStrategy.cs` 中添加定位器
4. 运行测试：确保新场景下的控件定位正确

## 许可证

本项目遵循原项目的许可证。

## 联系方式

如有问题或建议，请提交Issue或Pull Request。
