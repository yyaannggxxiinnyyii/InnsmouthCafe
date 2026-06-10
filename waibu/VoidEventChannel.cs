using UnityEngine;

/// <summary>
/// 无载荷事件频道：用于广播不携带数据的游戏事件（如"回合开始"、"按钮点击"）。
/// 在 Inspector 中拖入此 SO，调用 Raise() 即可通知所有订阅者。
/// </summary>
[CreateAssetMenu(menuName = "Events/Void Event Channel", fileName = "VoidEventChannel")]
public class VoidEventChannel : ScriptableObject
{
    private System.Collections.Generic.List<System.Action> _listeners
        = new System.Collections.Generic.List<System.Action>();

    /// <summary>
    /// 注册一个无参监听器。同一监听器不会被重复添加。
    /// </summary>
    /// <param name="listener">要注册的无参回调委托。</param>
    public void Subscribe(System.Action listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    /// <summary>
    /// 注销一个已注册的无参监听器。若未注册则无操作。
    /// </summary>
    /// <param name="listener">要注销的无参回调委托。</param>
    public void Unsubscribe(System.Action listener)
    {
        _listeners.Remove(listener);
    }

    /// <summary>
    /// 广播无载荷事件，依次调用所有已注册的监听器。
    /// 采用倒序遍历，支持监听器在回调中安全地注销自身。
    /// </summary>
    public void Raise()
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i]?.Invoke();
    }
}
