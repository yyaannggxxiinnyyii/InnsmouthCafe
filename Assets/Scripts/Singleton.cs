using UnityEngine;

/// <summary>
/// 泛型 MonoBehaviour 单例模板。
/// 用法：public class Foo : Singleton&lt;Foo&gt;
/// </summary>
/// <typeparam name="T">需要实现单例的 MonoBehaviour 子类</typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    /// <summary>
    /// 全局唯一实例访问器。场景中不存在时返回 null。
    /// </summary>
    public static T Instance => _instance;

    /// <summary>
    /// Unity 生命周期：初始化时注册单例。
    /// 若场景中已存在另一个实例，则销毁当前 GameObject。
    /// 子类覆写时必须调用 base.Awake()。
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
            _instance = this as T;
        else if (_instance != this)
            Destroy(gameObject);
    }
}
