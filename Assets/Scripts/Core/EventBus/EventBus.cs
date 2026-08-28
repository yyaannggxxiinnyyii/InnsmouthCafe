using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T> where T : IEvent {
    static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();
    
    public static void Register(IEventBinding<T> binding) => bindings.Add(binding);
    public static void Deregister(IEventBinding<T> binding) => bindings.Remove(binding);

    public static void Raise(T eventData) {
        var snapshot = new HashSet<IEventBinding<T>>(bindings);

        foreach (var binding in snapshot) {
            if (bindings.Contains(binding)) {
                binding.OnEvent.Invoke(eventData);
                binding.OnEventNoArgs.Invoke();
            }
        }
    }

    static void Clear() {
        Debug.Log($"Clearing {typeof(T).Name} bindings");
        bindings.Clear();
    }
}
