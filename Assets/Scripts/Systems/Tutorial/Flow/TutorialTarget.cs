using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教学目标标记组件，用稳定 ID 声明场景对象在引导流程中的身份。
/// </summary>
[DisallowMultipleComponent]
public class TutorialTarget : MonoBehaviour
{
    [Header("目标配置")]
    [SerializeField]
    [Tooltip("教学流程使用的稳定目标 ID")]
    private string _targetId;

    [SerializeField]
    [Tooltip("用于计算高亮区域的 RectTransform；为空时使用自身 RectTransform")]
    private RectTransform _highlightRect;

    [SerializeField]
    [Tooltip("高亮区域额外边距")]
    private float _padding = 20f;

    /// <summary>教学流程使用的稳定目标 ID。</summary>
    public string TargetId => _targetId;

    /// <summary>目标高亮区域。</summary>
    public RectTransform HighlightRect => _highlightRect != null ? _highlightRect : transform as RectTransform;

    /// <summary>高亮区域额外边距。</summary>
    public float Padding => _padding;

    private void OnEnable()
    {
        TutorialTargetRegistry.Instance?.Register(this);
    }

    private void OnDisable()
    {
        TutorialTargetRegistry.Instance?.Unregister(this);
    }
}