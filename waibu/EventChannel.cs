using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型事件频道基类，基于 ScriptableObject 实现发布-订阅模式。
/// 将事件发布方与订阅方完全解耦：双方只需引用同一个 SO 资产，互不依赖。
/// 频道资产在 ScriptableObjects/Events/ 目录下通过 [CreateAssetMenu] 子类创建。
/// </summary>
/// <typeparam name="T">事件携带的载荷数据类型。</typeparam>
public abstract class EventChannel<T> : ScriptableObject
{
    private readonly List<Action<T>> _listeners = new List<Action<T>>();

    /// <summary>
    /// 注册一个监听器，事件触发时将收到通知。
    /// 同一监听器不会被重复添加（内部做去重检查）。
    /// </summary>
    /// <param name="listener">要注册的回调委托。</param>
    public void Subscribe(Action<T> listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    /// <summary>
    /// 注销一个已注册的监听器。若该监听器未注册，则无操作。
    /// 建议在 MonoBehaviour.OnDisable 中调用，防止内存泄漏。
    /// </summary>
    /// <param name="listener">要注销的回调委托。</param>
    public void Unsubscribe(Action<T> listener)
    {
        _listeners.Remove(listener);
    }

    /// <summary>
    /// 广播事件，依次调用所有已注册的监听器。
    /// 采用倒序遍历，以支持监听器在回调中安全地注销自身。
    /// </summary>
    /// <param name="value">传递给所有监听器的载荷数据。</param>
    public void Raise(T value)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i]?.Invoke(value);
    }
}
