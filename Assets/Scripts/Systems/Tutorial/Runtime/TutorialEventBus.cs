using System;
using System.Collections.Generic;

/// <summary>
/// 教学事件总线
/// 任何系统都可以通过 Publish 发布事件，TutorialRunner 按步骤订阅需要等待的事件
/// 使用 List 存储监听器，Unsubscribe 按值匹配移除，无需保存订阅引用
/// </summary>
public static class TutorialEventBus
{
    private static readonly Dictionary<string, List<Action>> _listeners =
        new Dictionary<string, List<Action>>();

    /// <summary>
    /// 订阅事件，同一回调不会重复添加
    /// </summary>
    public static void Subscribe(string eventKey, Action callback)
    {
        if (string.IsNullOrEmpty(eventKey) || callback == null) return;

        if (!_listeners.TryGetValue(eventKey, out var list))
        {
            list = new List<Action>();
            _listeners[eventKey] = list;
        }

        if (!list.Contains(callback))
            list.Add(callback);
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    public static void Unsubscribe(string eventKey, Action callback)
    {
        if (string.IsNullOrEmpty(eventKey) || callback == null) return;

        if (_listeners.TryGetValue(eventKey, out var list))
            list.Remove(callback);
    }

    /// <summary>
    /// 发布事件，倒序遍历支持回调中安全注销自身
    /// </summary>
    public static void Publish(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey)) return;

        if (!_listeners.TryGetValue(eventKey, out var list)) return;

        for (int i = list.Count - 1; i >= 0; i--)
            list[i]?.Invoke();
    }

    /// <summary>
    /// 清空所有监听（场景卸载时调用）
    /// </summary>
    public static void Clear()
    {
        _listeners.Clear();
    }
}