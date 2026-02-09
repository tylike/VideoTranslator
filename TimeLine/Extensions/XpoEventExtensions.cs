using DevExpress.Xpo;
using System;
using System.Collections.Generic;

namespace TimeLine.Extensions;

public static class XpoEventExtensions
{
    #region XPCollection事件监听

    public static void SubscribeCollectionChanged(this XPCollection collection, XPCollectionChangedEventHandler handler)
    {
        if (collection == null || handler == null)
        {
            return;
        }

        collection.CollectionChanged += handler;
    }

    public static void UnsubscribeCollectionChanged(this XPCollection collection, XPCollectionChangedEventHandler handler)
    {
        if (collection == null || handler == null)
        {
            return;
        }

        collection.CollectionChanged -= handler;
    }

    #endregion

    #region XPBaseObject属性变化监听

    public static void SubscribePropertyChanged(this XPBaseObject obj, DevExpress.Xpo.ObjectChangeEventHandler handler)
    {
        if (obj == null || handler == null)
        {
            return;
        }

        obj.Changed += handler;
    }

    public static void UnsubscribePropertyChanged(this XPBaseObject obj, DevExpress.Xpo.ObjectChangeEventHandler handler)
    {
        if (obj == null || handler == null)
        {
            return;
        }

        obj.Changed -= handler;
    }

    #endregion

    #region 批量订阅/取消订阅

    public static void SubscribeAllProperties(this XPBaseObject obj, DevExpress.Xpo.ObjectChangeEventHandler handler)
    {
        if (obj == null || handler == null)
        {
            return;
        }

        obj.Changed += handler;
    }

    public static void UnsubscribeAllProperties(this XPBaseObject obj, DevExpress.Xpo.ObjectChangeEventHandler handler)
    {
        if (obj == null || handler == null)
        {
            return;
        }

        obj.Changed -= handler;
    }

    #endregion

    #region 集合批量订阅

    public static void SubscribeAllItems<T>(this XPCollection<T> collection, DevExpress.Xpo.ObjectChangeEventHandler handler) where T : XPBaseObject
    {
        if (collection == null || handler == null)
        {
            return;
        }

        foreach (var item in collection)
        {
            item.Changed += handler;
        }

        collection.CollectionChanged += (sender, e) =>
        {
            if (e.ChangedObject is T item)
            {
                var changeType = e.CollectionChangedType.ToString();
                
                if (changeType.Contains("Add") || changeType.Contains("Insert"))
                {
                    item.Changed += handler;
                }
                else if (changeType.Contains("Remove") || changeType.Contains("Delete"))
                {
                    item.Changed -= handler;
                }
            }
        };
    }

    public static void UnsubscribeAllItems<T>(this XPCollection<T> collection, DevExpress.Xpo.ObjectChangeEventHandler handler) where T : XPBaseObject
    {
        if (collection == null || handler == null)
        {
            return;
        }

        foreach (var item in collection)
        {
            item.Changed -= handler;
        }
    }

    #endregion
}
