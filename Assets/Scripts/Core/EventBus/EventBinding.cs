using System;

/// <summary>
/// 事件绑定接口，定义了带参数和不带参数的事件处理委托，定义了两个订阅者需要实现的回调。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IEventBinding<T> {
    public Action<T> OnEvent { get; set; }
    public Action OnEventNoArgs { get; set; }
}

public class EventBinding<T> : IEventBinding<T> where T : IEvent {
    // 两个委托默认为空方法，避免空引用异常
    Action<T> onEvent = _ => { };
    Action onEventNoArgs = () => { };

    /// <summary>
    /// 获取或设置带参数的事件处理委托
    /// </summary>
    Action<T> IEventBinding<T>.OnEvent {
        get => onEvent;
        set => onEvent = value;
    }

    /// <summary>
    /// 获取或设置不带参数的事件处理委托
    /// </summary>
    Action IEventBinding<T>.OnEventNoArgs {
        get => onEventNoArgs;
        set => onEventNoArgs = value;
    }

    // 两个构造函数，分别用于初始化带参数和不带参数的事件处理委托
    public EventBinding(Action<T> onEvent) => this.onEvent = onEvent;
    public EventBinding(Action onEventNoArgs) => this.onEventNoArgs = onEventNoArgs;
    
    public void Add(Action onEvent) => onEventNoArgs += onEvent;
    public void Remove(Action onEvent) => onEventNoArgs -= onEvent;
    
    public void Add(Action<T> onEvent) => this.onEvent += onEvent;
    public void Remove(Action<T> onEvent) => this.onEvent -= onEvent;
}