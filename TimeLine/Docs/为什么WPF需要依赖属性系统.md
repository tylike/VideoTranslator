# 为什么 WPF 需要依赖属性系统？

## 核心问题

**为什么 WPF 需要依赖属性（DependencyProperty）和依赖对象（DependencyObject），而不是只用 INotifyPropertyChanged 和 INotifyCollectionChanged？**

## 简短答案

**INotifyPropertyChanged 只能解决"属性变化通知"的问题，而 DependencyProperty 解决的是"整个属性系统"的问题。**

- INotifyPropertyChanged：简单的属性变化通知
- DependencyProperty：完整的属性系统，支持值继承、动画、样式、模板、优先级等

## 详细对比

### 1. INotifyPropertyChanged 的局限性

#### 示例：普通属性

```csharp
public class Person : INotifyPropertyChanged
{
    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

**局限性**：
- ❌ 只能通知属性变化
- ❌ 不支持值继承
- ❌ 不支持动画
- ❌ 不支持样式
- ❌ 不支持模板
- ❌ 不支持优先级系统
- ❌ 不支持默认值
- ❌ 不支持回调机制

### 2. DependencyProperty 的优势

#### 示例：依赖属性

```csharp
public class MyControl : Control
{
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register(
            nameof(Name),
            typeof(string),
            typeof(MyControl),
            new PropertyMetadata(string.Empty, OnNameChanged));

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MyControl)d;
        // 属性变化时的回调
        Console.WriteLine($"Name changed from {e.OldValue} to {e.NewValue}");
    }
}
```

**优势**：
- ✅ 支持属性变化通知
- ✅ 支持值继承
- ✅ 支持动画
- ✅ 支持样式
- ✅ 支持模板
- ✅ 支持优先级系统
- ✅ 支持默认值
- ✅ 支持回调机制

## 核心区别

### 1. 值继承（Value Inheritance）

#### INotifyPropertyChanged：不支持

```csharp
// ❌ 不支持值继承
public class Parent : INotifyPropertyChanged
{
    public string FontFamily { get; set; }
}

public class Child : INotifyPropertyChanged
{
    public string FontFamily { get; set; }  // 需要手动设置
}

// 使用
var parent = new Parent { FontFamily = "Arial" };
var child = new Child();
child.FontFamily = parent.FontFamily;  // ❌ 需要手动传递
```

#### DependencyProperty：支持

```xml
<!-- ✅ 支持值继承 -->
<Window FontFamily="Arial">
    <StackPanel>
        <TextBlock Text="Hello"/>  <!-- 自动继承 FontFamily -->
        <Button Content="Click"/>   <!-- 自动继承 FontFamily -->
        <TextBox/>                  <!-- 自动继承 FontFamily -->
    </StackPanel>
</Window>
```

**价值**：减少重复代码，统一管理样式。

### 2. 动画支持（Animation）

#### INotifyPropertyChanged：不支持

```csharp
// ❌ 不支持动画
public class MyControl : INotifyPropertyChanged
{
    private double _opacity;

    public double Opacity
    {
        get => _opacity;
        set
        {
            _opacity = value;
            OnPropertyChanged(nameof(Opacity));
        }
    }
}

// 想要做动画，需要手动实现
for (double i = 0; i <= 1; i += 0.01)
{
    control.Opacity = i;
    Thread.Sleep(10);
}
```

#### DependencyProperty：支持

```xml
<!-- ✅ 支持动画 -->
<Button Content="Click Me">
    <Button.Triggers>
        <EventTrigger RoutedEvent="MouseEnter">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                   From="1.0"
                                   To="0.5"
                                   Duration="0:0:0.2"/>
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </Button.Triggers>
</Button>
```

**价值**：提供流畅的动画效果，提升用户体验。

### 3. 样式支持（Style）

#### INotifyPropertyChanged：不支持

```csharp
// ❌ 不支持样式
public class MyControl : INotifyPropertyChanged
{
    public string Background { get; set; }
}

// 想要应用样式，需要手动设置
control.Background = "#FF0000";
```

#### DependencyProperty：支持

```xml
<!-- ✅ 支持样式 -->
<Style TargetType="Button">
    <Setter Property="Background" Value="#FF0000"/>
    <Setter Property="Foreground" Value="#FFFFFF"/>
    <Setter Property="FontWeight" Value="Bold"/>
</Style>

<!-- 所有 Button 自动应用样式 -->
<Button Content="Button 1"/>
<Button Content="Button 2"/>
<Button Content="Button 3"/>
```

**价值**：统一样式管理，提高代码可维护性。

### 4. 模板支持（Template）

#### INotifyPropertyChanged：不支持

```csharp
// ❌ 不支持模板
public class MyControl : INotifyPropertyChanged
{
    // 无法通过模板改变控件外观
}
```

#### DependencyProperty：支持

```xml
<!-- ✅ 支持模板 -->
<Style TargetType="Button">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="5">
                    <ContentPresenter HorizontalAlignment="Center"
                                    VerticalAlignment="Center"/>
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

**价值**：完全自定义控件外观，提高灵活性。

### 5. 优先级系统（Property Value Precedence）

#### INotifyPropertyChanged：不支持

```csharp
// ❌ 不支持优先级系统
public class MyControl : INotifyPropertyChanged
{
    public string Background { get; set; }
}

// 多个来源设置同一个属性，无法确定优先级
control.Background = "#FF0000";  // 本地设置
// 样式设置？模板设置？触发器设置？无法确定优先级
```

#### DependencyProperty：支持

```xml
<!-- ✅ 支持优先级系统 -->
<Style TargetType="Button">
    <Setter Property="Background" Value="#0000FF"/>  <!-- 样式 -->
</Style>

<Button Background="#FF0000">  <!-- 本地设置（优先级最高） -->
    <Button.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="#00FF00"/>  <!-- 触发器 -->
        </Trigger>
    </Button.Triggers>
</Button>
```

**优先级顺序（从高到低）**：
1. 动画
2. 本地值（Local Value）
3. 模板触发器
4. 样式触发器
5. 模板设置
6. 样式设置
7. 主题样式
8. 继承值
9. 默认值

**价值**：明确的优先级规则，避免冲突。

### 6. 默认值（Default Value）

#### INotifyPropertyChanged：需要手动实现

```csharp
// ❌ 需要手动实现默认值
public class MyControl : INotifyPropertyChanged
{
    private string _name = "Default Name";  // 手动设置默认值

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }
}
```

#### DependencyProperty：内置支持

```csharp
// ✅ 内置支持默认值
public static readonly DependencyProperty NameProperty =
    DependencyProperty.Register(
        nameof(Name),
        typeof(string),
        typeof(MyControl),
        new PropertyMetadata("Default Name"));  // 默认值
```

**价值**：集中管理默认值，减少代码。

### 7. 回调机制（Coerce Value Callback）

#### INotifyPropertyChanged：不支持

```csharp
// ❌ 不支持回调机制
public class MyControl : INotifyPropertyChanged
{
    private double _value;

    public double Value
    {
        get => _value;
        set
        {
            // 需要手动验证
            if (value < 0) value = 0;
            if (value > 100) value = 100;
            _value = value;
            OnPropertyChanged(nameof(Value));
        }
    }
}
```

#### DependencyProperty：支持

```csharp
// ✅ 支持回调机制
public static readonly DependencyProperty ValueProperty =
    DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(MyControl),
        new PropertyMetadata(0.0, null, CoerceValue));

private static object CoerceValue(DependencyObject d, object baseValue)
{
    var value = (double)baseValue;
    if (value < 0) return 0.0;
    if (value > 100) return 100.0;
    return value;
}
```

**价值**：集中管理验证逻辑，提高代码质量。

### 8. 性能优化（Performance）

#### INotifyPropertyChanged：每个对象独立存储

```csharp
// ❌ 每个对象独立存储属性值
public class Person : INotifyPropertyChanged
{
    private string _name;
    private int _age;
    private string _address;
    // 每个对象都有这些字段，占用内存
}

// 1000 个 Person 对象 = 3000 个字段
```

#### DependencyProperty：全局哈希表存储

```csharp
// ✅ 全局哈希表存储属性值
public class MyControl : DependencyObject
{
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register(nameof(Name), typeof(string), typeof(MyControl));

    // 属性值存储在全局哈希表中，减少内存占用
}

// 1000 个 MyControl 对象，共享同一个 DependencyProperty 定义
// 只有实际设置的值才会占用内存
```

**价值**：减少内存占用，提高性能。

## 实际应用示例

### 示例 1：数据绑定

#### INotifyPropertyChanged

```csharp
public class Person : INotifyPropertyChanged
{
    private string _name;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

```xml
<!-- 可以绑定 -->
<TextBlock Text="{Binding Name}"/>
```

#### DependencyProperty

```csharp
public class MyControl : Control
{
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register(nameof(Name), typeof(string), typeof(MyControl));

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }
}
```

```xml
<!-- 可以绑定 -->
<local:MyControl Name="John"/>
<local:MyControl Name="{Binding PersonName}"/>
```

**结论**：两者都支持数据绑定，但 DependencyProperty 提供更多功能。

### 示例 2：样式和主题

#### INotifyPropertyChanged：无法实现

```csharp
// ❌ 无法通过样式统一管理
public class MyButton : INotifyPropertyChanged
{
    public string Background { get; set; }
    public string Foreground { get; set; }
}

// 需要手动设置每个按钮
var button1 = new MyButton { Background = "#FF0000", Foreground = "#FFFFFF" };
var button2 = new MyButton { Background = "#FF0000", Foreground = "#FFFFFF" };
var button3 = new MyButton { Background = "#FF0000", Foreground = "#FFFFFF" };
```

#### DependencyProperty：可以轻松实现

```xml
<!-- ✅ 通过样式统一管理 -->
<Style TargetType="Button">
    <Setter Property="Background" Value="#FF0000"/>
    <Setter Property="Foreground" Value="#FFFFFF"/>
</Style>

<!-- 所有 Button 自动应用样式 -->
<Button Content="Button 1"/>
<Button Content="Button 2"/>
<Button Content="Button 3"/>
```

### 示例 3：动画效果

#### INotifyPropertyChanged：需要手动实现

```csharp
// ❌ 需要手动实现动画
public class MyControl : INotifyPropertyChanged
{
    private double _opacity;

    public double Opacity
    {
        get => _opacity;
        set
        {
            _opacity = value;
            OnPropertyChanged(nameof(Opacity));
        }
    }
}

// 手动实现动画
Task.Run(async () =>
{
    for (double i = 1.0; i >= 0.5; i -= 0.01)
    {
        Application.Current.Dispatcher.Invoke(() => control.Opacity = i);
        await Task.Delay(10);
    }
});
```

#### DependencyProperty：内置支持

```xml
<!-- ✅ 内置支持动画 -->
<Button Content="Click Me">
    <Button.Triggers>
        <EventTrigger RoutedEvent="MouseEnter">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                   From="1.0"
                                   To="0.5"
                                   Duration="0:0:0.2"/>
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </Button.Triggers>
</Button>
```

## 何时使用哪种？

### 使用 INotifyPropertyChanged

**适用场景**：
- ✅ 纯数据模型（Model）
- ✅ 不需要 UI 相关功能
- ✅ 不需要值继承、动画、样式等
- ✅ 跨平台使用（如 Xamarin、MAUI）

**示例**：
```csharp
public class Person : INotifyPropertyChanged
{
    public string Name { get; set; }
    public int Age { get; set; }
    // 纯数据模型，不需要 UI 功能
}
```

### 使用 DependencyProperty

**适用场景**：
- ✅ UI 控件（Control）
- ✅ 需要样式、模板、动画
- ✅ 需要值继承
- ✅ 需要数据绑定
- ✅ 需要优先级系统

**示例**：
```csharp
public class MyControl : Control
{
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register(nameof(Name), typeof(string), typeof(MyControl));

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }
}
```

## 总结

### 核心区别

| 特性 | INotifyPropertyChanged | DependencyProperty |
|------|----------------------|-------------------|
| 属性变化通知 | ✅ 支持 | ✅ 支持 |
| 值继承 | ❌ 不支持 | ✅ 支持 |
| 动画 | ❌ 不支持 | ✅ 支持 |
| 样式 | ❌ 不支持 | ✅ 支持 |
| 模板 | ❌ 不支持 | ✅ 支持 |
| 优先级系统 | ❌ 不支持 | ✅ 支持 |
| 默认值 | ❌ 需要手动实现 | ✅ 内置支持 |
| 回调机制 | ❌ 不支持 | ✅ 支持 |
| 性能优化 | ❌ 每个对象独立存储 | ✅ 全局哈希表存储 |
| 跨平台 | ✅ 支持 | ❌ 仅 WPF |

### 为什么 WPF 需要依赖属性？

**因为 WPF 的设计目标是提供一个完整的 UI 框架，而不仅仅是数据绑定。**

1. **UI 框架需要的功能**：
   - 样式（Style）
   - 模板（Template）
   - 动画（Animation）
   - 值继承（Value Inheritance）
   - 优先级系统（Property Value Precedence）
   - 主题（Theme）

2. **INotifyPropertyChanged 只能解决**：
   - 属性变化通知
   - 数据绑定

3. **DependencyProperty 解决**：
   - 属性变化通知
   - 数据绑定
   - 样式、模板、动画
   - 值继承、优先级系统
   - 主题

### 最佳实践

```csharp
// ✅ 数据模型使用 INotifyPropertyChanged
public class Person : INotifyPropertyChanged
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// ✅ UI 控件使用 DependencyProperty
public class MyControl : Control
{
    public static readonly DependencyProperty PersonProperty =
        DependencyProperty.Register(nameof(Person), typeof(Person), typeof(MyControl));

    public Person Person
    {
        get => (Person)GetValue(PersonProperty);
        set => SetValue(PersonProperty, value);
    }
}
```

**结论**：
- INotifyPropertyChanged：用于数据模型
- DependencyProperty：用于 UI 控件
- 两者互补，不是替代关系
