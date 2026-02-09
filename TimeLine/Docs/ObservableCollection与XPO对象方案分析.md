# 方案分析：ObservableCollection + XPO 对象

## 方案描述

```csharp
public class TimeLineModel
{
    // 集合使用 ObservableCollection
    public ObservableCollection<TrackInfo> Rows { get; } = new();

    // 单个对象直接使用 XPO 对象
    // TrackInfo 继承自 XPBaseObject
}

public class TrackInfo : XPObject
{
    public TrackInfo(Session session) : base(session)
    {
    }

    public string Title
    {
        get => GetPropertyValue<string>(nameof(Title));
        set => SetPropertyValue(nameof(Title), value);
    }
}
```

## 问题分析

### ❌ 问题 1：数据不会自动同步到数据库

```csharp
var model = new TimeLineModel();
var session = new Session();

// 添加元素到 ObservableCollection
model.Rows.Add(new TrackInfo(session) { Title = "Track 1" });
model.Rows.Add(new TrackInfo(session) { Title = "Track 2" });

// ❌ 问题：这些对象只存在于内存中，不会自动保存到数据库
// 需要手动调用 session.Save()
session.Save();  // 需要手动保存
```

### ❌ 问题 2：删除元素不会从数据库删除

```csharp
// 从 ObservableCollection 删除元素
model.Rows.RemoveAt(0);

// ❌ 问题：元素只是从集合中移除，但不会从数据库删除
// 需要手动调用 session.Delete()
session.Delete(removedItem);  // 需要手动删除
```

### ❌ 问题 3：与 XPCollection 不同步

```csharp
var project = new VideoProject(session);
var xpCollection = project.Tracks;  // XPCollection

var model = new TimeLineModel();

// 从 XPCollection 复制到 ObservableCollection
foreach (var track in xpCollection)
{
    model.Rows.Add(track);
}

// ❌ 问题：两个集合是独立的，修改一个不会影响另一个
model.Rows.Add(new TrackInfo(session) { Title = "New Track" });
// xpCollection 中没有这个新元素

xpCollection.Add(new TrackInfo(session) { Title = "Another Track" });
// model.Rows 中没有这个新元素
```

### ❌ 问题 4：Session 管理混乱

```csharp
var session1 = new Session();
var session2 = new Session();

var track1 = new TrackInfo(session1) { Title = "Track 1" };
var track2 = new TrackInfo(session2) { Title = "Track 2" };

// ❌ 问题：不同 Session 的对象不能放在同一个集合中
model.Rows.Add(track1);
model.Rows.Add(track2);  // 可能导致问题
```

### ⚠️ 问题 5：属性变更通知

```csharp
// XPO 对象会触发 PropertyChanged
model.Rows[0].Title = "New Title";  // ✓ 会触发 PropertyChanged

// 但 ObservableCollection 不会触发 CollectionChanged
// 这通常不是问题，因为 UI 会自动监听元素的 PropertyChanged
```

## 解决方案

### 方案 A：手动同步（不推荐）

```csharp
public class TimeLineModel
{
    private readonly Session _session;
    private readonly XPCollection<TrackInfo> _xpCollection;

    public ObservableCollection<TrackInfo> Rows { get; } = new();

    public TimeLineModel(VideoProject project)
    {
        _session = project.Session;
        _xpCollection = project.Tracks;

        // 初始同步
        SyncFromXPO();

        // 监听 ObservableCollection 变化，同步到 XPO
        Rows.CollectionChanged += OnRowsCollectionChanged;
    }

    private void SyncFromXPO()
    {
        Rows.Clear();
        foreach (var track in _xpCollection)
        {
            Rows.Add(track);
        }
    }

    private void OnRowsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (TrackInfo track in e.NewItems)
                    {
                        _xpCollection.Add(track);
                        _session.Save(track);  // 手动保存
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (TrackInfo track in e.OldItems)
                    {
                        _xpCollection.Remove(track);
                        _session.Delete(track);  // 手动删除
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                _xpCollection.Clear();
                foreach (var track in Rows)
                {
                    _xpCollection.Add(track);
                }
                break;
        }
    }
}
```

**缺点**：
- 需要手动管理同步逻辑
- 容易出错
- 代码复杂

### 方案 B：使用适配器（推荐）

```csharp
public class XPOCollectionAdapter<T> : ObservableCollection<T> where T : XPBaseObject
{
    private readonly XPCollection<T> _xpCollection;
    private readonly Session _session;
    private bool _isUpdating;

    public XPOCollectionAdapter(XPCollection<T> xpCollection)
    {
        _xpCollection = xpCollection ?? throw new ArgumentNullException(nameof(xpCollection));
        _session = xpCollection.Session;

        SyncFromXPO();
        _xpCollection.CollectionChanged += OnXPOCollectionChanged;
    }

    private void SyncFromXPO()
    {
        _isUpdating = true;
        try
        {
            Clear();
            foreach (var item in _xpCollection)
            {
                Add(item);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnXPOCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isUpdating) return;

        _isUpdating = true;
        try
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (T item in e.NewItems)
                        {
                            Add(item);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (T item in e.OldItems)
                        {
                            Remove(item);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    SyncFromXPO();
                    break;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    protected override void InsertItem(int index, T item)
    {
        if (!_isUpdating)
        {
            _xpCollection.Insert(index, item);
            _session.Save(item);  // 自动保存
        }
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        if (!_isUpdating)
        {
            var item = this[index];
            _xpCollection.RemoveAt(index);
            _session.Delete(item);  // 自动删除
        }
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        if (!_isUpdating)
        {
            foreach (var item in this)
            {
                _session.Delete(item);
            }
            _xpCollection.Clear();
        }
        base.ClearItems();
    }
}

public class TimeLineModel
{
    private readonly XPOCollectionAdapter<TrackInfo> _rowsAdapter;

    public ObservableCollection<TrackInfo> Rows => _rowsAdapter;

    public TimeLineModel(VideoProject project)
    {
        _rowsAdapter = new XPOCollectionAdapter<TrackInfo>(project.Tracks);
    }
}
```

**优点**：
- 自动同步，无需手动管理
- 封装了复杂的同步逻辑
- 易于维护和测试

### 方案 C：直接使用 XPCollection（简单场景）

```csharp
public class TimeLineModel
{
    public XPCollection<TrackInfo> Rows { get; }

    public TimeLineModel(VideoProject project)
    {
        Rows = project.Tracks;
    }
}
```

**优点**：
- 最简单，无需适配器
- 数据自动同步到数据库
- XPCollection 实现了 INotifyCollectionChanged，可以直接用于 WPF 绑定

**缺点**：
- 某些场景下可能需要额外处理
- 性能可能不如 ObservableCollection

## 推荐方案

### 场景 1：只读显示或简单操作
**推荐**：直接使用 `XPCollection`

```csharp
public class TimeLineModel
{
    public XPCollection<TrackInfo> Rows { get; }

    public TimeLineModel(VideoProject project)
    {
        Rows = project.Tracks;
    }
}
```

### 场景 2：需要频繁修改集合
**推荐**：使用 `XPOCollectionAdapter`

```csharp
public class TimeLineModel
{
    private readonly XPOCollectionAdapter<TrackInfo> _rowsAdapter;

    public ObservableCollection<TrackInfo> Rows => _rowsAdapter;

    public TimeLineModel(VideoProject project)
    {
        _rowsAdapter = new XPOCollectionAdapter<TrackInfo>(project.Tracks);
    }
}
```

### 场景 3：需要精确控制
**推荐**：手动管理同步逻辑

```csharp
public class TimeLineModel
{
    public ObservableCollection<TrackInfo> Rows { get; } = new();

    public void AddTrack(TrackInfo track)
    {
        Rows.Add(track);
        track.Session.Save(track);  // 手动保存
    }

    public void RemoveTrack(TrackInfo track)
    {
        Rows.Remove(track);
        track.Session.Delete(track);  // 手动删除
    }
}
```

## 总结

### 问题：集合属性用 ObservableCollection，单个对象直接用 XPO 对象，是不是没有问题了？

**答案**：❌ **不是没有问题，而是需要解决数据同步问题。**

### 主要问题

1. ❌ 数据不会自动保存到数据库
2. ❌ 删除元素不会从数据库删除
3. ❌ 与 XPCollection 不同步
4. ❌ Session 管理混乱
5. ⚠️ 属性变更通知（通常不是问题）

### 推荐方案

| 场景 | 推荐方案 | 说明 |
|------|---------|------|
| 只读显示 | 直接使用 XPCollection | 最简单，无需适配器 |
| 简单操作 | 直接使用 XPCollection | XPCollection 支持 WPF 绑定 |
| 频繁修改 | 使用 XPOCollectionAdapter | 自动同步，无需手动管理 |
| 精确控制 | 手动管理同步逻辑 | 最大灵活性 |

### 最佳实践

```csharp
// ✅ 推荐：使用适配器
public class TimeLineModel
{
    private readonly XPOCollectionAdapter<TrackInfo> _rowsAdapter;

    public ObservableCollection<TrackInfo> Rows => _rowsAdapter;

    public TimeLineModel(VideoProject project)
    {
        _rowsAdapter = new XPOCollectionAdapter<TrackInfo>(project.Tracks);
    }
}

// ✅ 也可以：直接使用 XPCollection
public class TimeLineModel
{
    public XPCollection<TrackInfo> Rows { get; }

    public TimeLineModel(VideoProject project)
    {
        Rows = project.Tracks;
    }
}

// ❌ 不推荐：直接使用 ObservableCollection + XPO 对象
public class TimeLineModel
{
    public ObservableCollection<TrackInfo> Rows { get; } = new();
    // 数据不会自动同步到数据库
}
```
