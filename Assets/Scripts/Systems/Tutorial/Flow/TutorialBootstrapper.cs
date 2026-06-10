using InnsmouthCafe.Data;
using UnityEngine;

/// <summary>
/// 教学系统启动器，监听开场完成事件并在教学模式第一天启动咖啡教学。
/// </summary>
[DisallowMultipleComponent]
public class TutorialBootstrapper : MonoBehaviour
{
    [Header("播放规则")]
    [SerializeField]
    [Tooltip("是否在满足条件时自动播放教学")]
    private bool _autoPlay = true;

    [SerializeField]
    [Tooltip("自动播放时是否检查教学完成标记")]
    private bool _respectCompletionFlag = true;

    private void Awake()
    {
        EnsureRuntimeObject<TutorialTargetRegistry>("TutorialTargetRegistry");
        EnsureRuntimeObject<TutorialRunner>("TutorialRunner");
    }

    private void OnEnable()
    {
        TutorialEventBus.Subscribe(TutorialEvents.OctopusOpeningComplete, OnOpeningComplete);
    }

    private void OnDisable()
    {
        TutorialEventBus.Unsubscribe(TutorialEvents.OctopusOpeningComplete, OnOpeningComplete);
    }

    /// <summary>
    /// 场景加载后确保存在启动器，避免旧系统对象删除后失去自动入口。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBootstrapper()
    {
        EnsureRuntimeObject<TutorialBootstrapper>("TutorialBootstrapper");
    }

    /// <summary>
    /// 播放咖啡教学；forceRepeat 为 true 时忽略完成标记，便于调试和重看。
    /// </summary>
    public void PlayCoffeeTutorial(bool forceRepeat = false)
    {
        if (!forceRepeat && !CanAutoPlay())
            return;

        TutorialRunner.Instance?.PlayCoffeeTutorial(forceRepeat);
    }

    /// <summary>
    /// 处理章鱼老板开场结束事件，满足条件时自动播放教学。
    /// </summary>
    private void OnOpeningComplete()
    {
        if (_autoPlay)
        {
            TutorialGate.Reset();
            PlayCoffeeTutorial(forceRepeat: false);
        }
    }

    /// <summary>
    /// 判断当前是否允许自动播放教学，避免普通模式或已完成教学时误触发。
    /// </summary>
    private bool CanAutoPlay()
    {
        if (GameManager.Instance == null || GameManager.Instance.SelectedModeConfig == null)
            return false;

        if (GameManager.Instance.SelectedModeConfig.gameMode != GameMode.Tutorial)
            return false;

        if (GameFlowManager.Instance == null || GameFlowManager.Instance.CurrentDay != 1)
            return false;

        if (_respectCompletionFlag && GameManager.Instance.IsTutorialCompleted())
            return false;

        return true;
    }

    /// <summary>
    /// 确保指定运行时组件存在，不存在时创建独立 GameObject 承载。
    /// </summary>
    private static void EnsureRuntimeObject<T>(string objectName) where T : MonoBehaviour
    {
        if (FindObjectOfType<T>(true) != null)
            return;

        GameObject gameObject = new GameObject(objectName);
        gameObject.AddComponent<T>();
    }
}
