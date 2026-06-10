using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 教学引导步骤的输入推进方式。
/// </summary>
public enum TutorialGuideInteraction
{
    None,
    AnyClick,
    AreaClick,
    TargetPassThrough
}

/// <summary>
/// 教学引导 UI，负责遮罩、高亮、对话框、打字机和强制点击区域判断。
/// </summary>
public class TutorialGuideUI : MonoBehaviour
{
    [Header("整体控制")]
    [SerializeField]
    [Tooltip("控制引导 UI 显隐和输入拦截的 CanvasGroup")]
    private CanvasGroup _maskGroup;

    [Header("高亮区域")]
    [SerializeField]
    [Tooltip("用于显示高亮镂空区域的 RectTransform")]
    private RectTransform _highlightArea;

    [Header("老板对话框")]
    [SerializeField]
    [Tooltip("对话框根节点")]
    private RectTransform _dialogueRoot;

    [SerializeField]
    [Tooltip("对话框 CanvasGroup")]
    private CanvasGroup _dialogueGroup;

    [SerializeField]
    [Tooltip("对话正文文本")]
    private TextMeshProUGUI _dialogueText;

    [SerializeField]
    [Tooltip("继续按钮")]
    private Button _continueButton;

    [Header("动画设置")]
    [SerializeField]
    [Tooltip("淡入淡出时长")]
    private float _fadeDuration = 0.2f;

    [SerializeField]
    [Tooltip("对话框弹出缩放时长")]
    private float _popDuration = 0.25f;

    [Header("对话框定位")]
    [SerializeField]
    [Tooltip("对话框与高亮区域之间的间距")]
    private float _dialogueOffset = 20f;

    [SerializeField]
    [Tooltip("对话框与窗口边缘之间的最小安全距离")]
    private float _dialogueScreenPadding = 20f;

    [Header("打字机设置")]
    [SerializeField]
    [Tooltip("每个字符显示间隔，使用真实时间")]
    private float _typeCharDelay = 0.02f;

    /// <summary>当前步骤完成回调。</summary>
    private Action _onStepComplete;

    /// <summary>玩家点中指定区域时的回调。</summary>
    private Action _onAreaClick;

    /// <summary>当前步骤输入推进方式。</summary>
    private TutorialGuideInteraction _interactionMode = TutorialGuideInteraction.AnyClick;

    /// <summary>当前可点击区域目标。</summary>
    private RectTransform _interactionTarget;

    /// <summary>当前可点击区域边距。</summary>
    private float _interactionPadding;

    /// <summary>当前所在 Canvas 的 RectTransform。</summary>
    private RectTransform _canvasRect;

    /// <summary>Canvas 对应的相机。</summary>
    private Camera _canvasCamera;

    /// <summary>遮罩点击事件组件。</summary>
    private EventTrigger _maskEventTrigger;

    /// <summary>当前打字机协程。</summary>
    private Coroutine _typeCoroutine;

    /// <summary>当前完整文本。</summary>
    private string _currentFullText = string.Empty;

    /// <summary>是否正在打字。</summary>
    private bool _isTyping;

    /// <summary>完整文本是否已经显示。</summary>
    private bool _isFullTextShown;

    private void Awake()
    {
        CacheCanvas();
        ConfigureRaycastBlocking();
        BindContinueButton();
        BindMaskClick();
        HideAll(immediate: true);
    }

    /// <summary>
    /// 显示通用教学步骤，并按指定交互方式等待玩家操作。
    /// </summary>
    public void ShowStep(
        string text,
        RectTransform target,
        float padding,
        TutorialGuideInteraction interactionMode,
        Action onAdvance,
        Action onAreaClick = null)
    {
        _onStepComplete = onAdvance;
        _onAreaClick = onAreaClick;
        _interactionMode = interactionMode;
        _interactionTarget = target;
        _interactionPadding = padding;
        _currentFullText = text ?? string.Empty;
        _isTyping = false;
        _isFullTextShown = false;

        PrepareDialogueLayoutForPositioning();
        PositionHighlight(target, padding);
        ShowMask();
        ShowDialoguePanel();
        BeginTypingCurrentText();

        if (_continueButton != null)
            _continueButton.gameObject.SetActive(interactionMode == TutorialGuideInteraction.AnyClick);
    }

    /// <summary>
    /// 显示纯文本提示，用于兼容外部直接提示调用。
    /// </summary>
    public void ShowTip(string text)
    {
        ShowStep(text, null, 0f, TutorialGuideInteraction.AnyClick, null);
    }

    /// <summary>
    /// 隐藏当前提示对话框。
    /// </summary>
    public void HideTip()
    {
        HideTipPanel();
    }

    /// <summary>
    /// 显示指定目标的高亮区域。
    /// </summary>
    public void ShowHighlight(RectTransform target, float padding = 20f)
    {
        if (target == null)
            return;

        PositionHighlight(target, padding);
        ShowMask();
    }

    /// <summary>
    /// 隐藏高亮遮罩。
    /// </summary>
    public void HideHighlight()
    {
        ResetHighlight();
        HideMask();
    }

    /// <summary>
    /// 外部强制完成当前步骤。
    /// </summary>
    public void CompleteCurrentStep()
    {
        OnContinueClicked();
    }

    /// <summary>
    /// 隐藏全部教学 UI 并清理当前步骤状态。
    /// </summary>
    public void HideAll(bool immediate = false)
    {
        StopTyping();
        ResetHighlight();

        if (immediate)
        {
            SetMaskVisible(false, true);
            SetGroupVisible(_dialogueGroup, false);

            if (_dialogueRoot != null)
                _dialogueRoot.gameObject.SetActive(false);
        }
        else
        {
            HideMask();
            HideTipPanel();
        }

        _onStepComplete = null;
        _onAreaClick = null;
        _interactionMode = TutorialGuideInteraction.AnyClick;
        _interactionTarget = null;
        _interactionPadding = 0f;
        _currentFullText = string.Empty;
        _isTyping = false;
        _isFullTextShown = false;

        if (_continueButton != null)
            _continueButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 缓存 Canvas 和相机引用。
    /// </summary>
    private void CacheCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        _canvasRect = canvas.GetComponent<RectTransform>();
        _canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    /// <summary>
    /// 绑定继续按钮点击事件。
    /// </summary>
    private void BindContinueButton()
    {
        if (_continueButton != null)
            _continueButton.onClick.AddListener(OnContinueClicked);
    }

    /// <summary>
    /// 给遮罩绑定点击事件，用于任意点击和区域点击判断。
    /// </summary>
    private void BindMaskClick()
    {
        if (_maskGroup == null)
            return;

        _maskEventTrigger = _maskGroup.GetComponent<EventTrigger>();
        if (_maskEventTrigger == null)
            _maskEventTrigger = _maskGroup.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry clickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        clickEntry.callback.AddListener(OnMaskPointerClick);
        _maskEventTrigger.triggers.Add(clickEntry);
    }

    /// <summary>
    /// 配置遮罩和高亮的射线状态，确保强引导输入统一由遮罩处理。
    /// </summary>
    private void ConfigureRaycastBlocking()
    {
        if (_maskGroup != null)
        {
            Graphic maskGraphic = _maskGroup.GetComponent<Graphic>();
            if (maskGraphic == null)
            {
                Image image = _maskGroup.gameObject.AddComponent<Image>();
                image.color = Color.clear;
                maskGraphic = image;
            }

            maskGraphic.raycastTarget = true;
            StretchToCanvas(_maskGroup.transform as RectTransform);

            TutorialMaskRaycastFilter filter = _maskGroup.GetComponent<TutorialMaskRaycastFilter>();
            if (filter == null)
                filter = _maskGroup.gameObject.AddComponent<TutorialMaskRaycastFilter>();

            filter.Initialize(this);
        }

        SetRaycastTarget(_highlightArea, false);
    }

    /// <summary>
    /// 将 RectTransform 拉伸到父节点全屏，保证遮罩能覆盖整个教学 Canvas。
    /// </summary>
    private void StretchToCanvas(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 设置指定节点及其子节点 Graphic 是否接收射线。
    /// </summary>
    private void SetRaycastTarget(RectTransform root, bool raycastTarget)
    {
        if (root == null)
            return;

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = raycastTarget;
    }

    /// <summary>
    /// 将高亮区域移动到目标位置；无目标时使用全屏区域。
    /// </summary>
    private void PositionHighlight(RectTransform target, float padding)
    {
        if (_highlightArea == null)
            return;

        _highlightArea.gameObject.SetActive(true);

        if (target == null)
        {
            _highlightArea.anchoredPosition = Vector2.zero;
            _highlightArea.sizeDelta = _canvasRect != null
                ? _canvasRect.rect.size
                : new Vector2(Screen.width, Screen.height);

            if (_dialogueRoot != null)
            {
                GetCanvasScreenBounds(out Vector2 canvasMin, out Vector2 canvasMax);
                PositionDialogueNextToHighlight(canvasMin, canvasMax, 0f);
            }

            return;
        }

        RectTransform parent = _highlightArea.parent as RectTransform;
        if (parent == null)
            return;

        GetTargetLocalRect(target, parent, padding, out Vector2 center, out Vector2 size, out Vector2 screenMin, out Vector2 screenMax);
        _highlightArea.anchoredPosition = center;
        _highlightArea.sizeDelta = size;

        if (_dialogueRoot != null)
            PositionDialogueNextToHighlight(screenMin, screenMax, padding);
    }

    /// <summary>
    /// 计算目标在高亮父节点下的中心、尺寸和屏幕范围。
    /// </summary>
    private void GetTargetLocalRect(
        RectTransform target,
        RectTransform parent,
        float padding,
        out Vector2 center,
        out Vector2 size,
        out Vector2 screenMin,
        out Vector2 screenMax)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 localMin = Vector2.positiveInfinity;
        Vector2 localMax = Vector2.negativeInfinity;
        screenMin = Vector2.positiveInfinity;
        screenMax = Vector2.negativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_canvasCamera, corners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, _canvasCamera, out Vector2 localPoint);

            localMin = Vector2.Min(localMin, localPoint);
            localMax = Vector2.Max(localMax, localPoint);
            screenMin = Vector2.Min(screenMin, screenPoint);
            screenMax = Vector2.Max(screenMax, screenPoint);
        }

        center = (localMin + localMax) * 0.5f;
        size = (localMax - localMin) + Vector2.one * padding * 2f;
    }

    /// <summary>
    /// 显示遮罩并启用输入拦截。
    /// </summary>
    private void ShowMask()
    {
        SetMaskVisible(true, false);
    }

    /// <summary>
    /// 隐藏遮罩并关闭输入拦截。
    /// </summary>
    private void HideMask()
    {
        SetMaskVisible(false, false);
    }

    /// <summary>
    /// 重置高亮区域，避免教学结束后残留可见高亮。
    /// </summary>
    private void ResetHighlight()
    {
        if (_highlightArea == null)
            return;

        _highlightArea.anchoredPosition = Vector2.zero;
        _highlightArea.sizeDelta = Vector2.zero;
        _highlightArea.gameObject.SetActive(false);
    }

    /// <summary>
    /// 按指定状态设置遮罩显隐。
    /// </summary>
    private void SetMaskVisible(bool visible, bool immediate)
    {
        if (_maskGroup == null)
            return;

        _maskGroup.DOKill();
        _maskGroup.interactable = visible;
        _maskGroup.blocksRaycasts = visible;

        if (immediate)
        {
            _maskGroup.alpha = visible ? 1f : 0f;
            return;
        }

        _maskGroup.DOFade(visible ? 1f : 0f, _fadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (!visible)
                {
                    _maskGroup.interactable = false;
                    _maskGroup.blocksRaycasts = false;
                }
            });
    }

    /// <summary>
    /// 显示对话框、设置说话者并播放弹出动画。
    /// </summary>
    private void ShowDialoguePanel()
    {
        if (_dialogueRoot != null)
            _dialogueRoot.gameObject.SetActive(true);

        if (_dialogueGroup != null)
        {
            _dialogueGroup.DOKill();
            _dialogueGroup.alpha = 0f;
            _dialogueGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
        }

        if (_dialogueRoot != null)
        {
            _dialogueRoot.DOKill();
            _dialogueRoot.localScale = Vector3.one * 0.8f;
            _dialogueRoot.DOScale(Vector3.one, _popDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    /// <summary>
    /// 隐藏对话框。
    /// </summary>
    private void HideTipPanel()
    {
        if (_dialogueGroup == null)
            return;

        _dialogueGroup.DOKill();
        _dialogueGroup.DOFade(0f, _fadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (_dialogueRoot != null)
                    _dialogueRoot.gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// 点击继续按钮时推进步骤或先显示完整文本。
    /// </summary>
    private void OnContinueClicked()
    {
        if (_isTyping || !_isFullTextShown)
        {
            ShowFullCurrentText();
            return;
        }

        if (_interactionMode != TutorialGuideInteraction.AnyClick)
            return;

        Action callback = _onStepComplete;
        _onStepComplete = null;

        HideAll();
        callback?.Invoke();
    }

    /// <summary>
    /// 处理遮罩点击，根据当前交互模式决定推进或区域命中。
    /// </summary>
    private void OnMaskPointerClick(BaseEventData eventData)
    {
        if (!isActiveAndEnabled || _interactionMode == TutorialGuideInteraction.None)
            return;

        if (_isTyping || !_isFullTextShown)
        {
            ShowFullCurrentText();
            return;
        }

        if (_interactionMode == TutorialGuideInteraction.AnyClick)
        {
            OnContinueClicked();
            return;
        }

        if (_interactionMode == TutorialGuideInteraction.TargetPassThrough)
            return;

        if (_interactionMode != TutorialGuideInteraction.AreaClick || !(eventData is PointerEventData pointerEventData))
            return;

        if (_interactionTarget == null || IsScreenPointInsideTarget(_interactionTarget, pointerEventData.position, _interactionPadding))
            _onAreaClick?.Invoke();
    }

    /// <summary>
    /// 判断遮罩是否接收当前射线；目标透传模式下，高亮区域内不接收射线。
    /// </summary>
    public bool ShouldMaskReceiveRaycast(Vector2 screenPoint)
    {
        if (_interactionMode != TutorialGuideInteraction.TargetPassThrough)
            return true;

        if (_isTyping || !_isFullTextShown)
            return true;

        if (_interactionTarget == null)
            return true;

        return !IsScreenPointInsideTarget(_interactionTarget, screenPoint, _interactionPadding);
    }

    /// <summary>
    /// 判断屏幕点击点是否位于目标区域内。
    /// </summary>
    private bool IsScreenPointInsideTarget(RectTransform target, Vector2 screenPoint, float padding)
    {
        if (target == null)
            return false;

        GetTargetScreenBounds(target, padding, out Vector2 min, out Vector2 max);
        return screenPoint.x >= min.x
            && screenPoint.x <= max.x
            && screenPoint.y >= min.y
            && screenPoint.y <= max.y;
    }

    /// <summary>
    /// 获取目标屏幕坐标范围。
    /// </summary>
    private void GetTargetScreenBounds(RectTransform target, float padding, out Vector2 min, out Vector2 max)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        min = Vector2.positiveInfinity;
        max = Vector2.negativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(_canvasCamera, corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        min -= Vector2.one * padding;
        max += Vector2.one * padding;
    }

    /// <summary>
    /// 获取目标屏幕中心点。
    /// </summary>
    private Vector2 GetTargetScreenCenter(RectTransform target)
    {
        GetTargetScreenBounds(target, 0f, out Vector2 min, out Vector2 max);
        return (min + max) * 0.5f;
    }

    /// <summary>
    /// 在定位前刷新对话框布局，确保边界判断使用当前文案对应的尺寸。
    /// </summary>
    private void PrepareDialogueLayoutForPositioning()
    {
        if (_dialogueRoot == null)
            return;

        _dialogueRoot.gameObject.SetActive(true);
        _dialogueRoot.localScale = Vector3.one;

        if (_dialogueText != null)
            _dialogueText.text = _currentFullText;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_dialogueRoot);
    }

    /// <summary>
    /// 根据高亮区域位置自动摆放对话框。
    /// </summary>
    private void PositionDialogueNextToHighlight(Vector2 screenMin, Vector2 screenMax, float padding)
    {
        if (_dialogueRoot == null)
            return;

        RectTransform parent = _dialogueRoot.parent as RectTransform;
        if (parent == null)
            return;

        GetCanvasScreenBounds(out Vector2 canvasMin, out Vector2 canvasMax);
        canvasMin += Vector2.one * _dialogueScreenPadding;
        canvasMax -= Vector2.one * _dialogueScreenPadding;

        Vector2 highlightMin = screenMin - Vector2.one * padding;
        Vector2 highlightMax = screenMax + Vector2.one * padding;
        Vector2 highlightCenter = (highlightMin + highlightMax) * 0.5f;
        GetDialogueScreenExtents(out float left, out float right, out float bottom, out float top);

        Vector2[] candidates =
        {
            new Vector2(highlightCenter.x, highlightMin.y - _dialogueOffset - top),
            new Vector2(highlightMin.x - _dialogueOffset - right, highlightCenter.y),
            new Vector2(highlightCenter.x, highlightMax.y + _dialogueOffset + bottom),
            new Vector2(highlightMax.x + _dialogueOffset + left, highlightCenter.y)
        };

        Vector2 preferredScreenPos = candidates[0];
        for (int i = 0; i < candidates.Length; i++)
        {
            if (!IsDialogueInsideScreenBounds(candidates[i], canvasMin, canvasMax, left, right, bottom, top))
                continue;

            preferredScreenPos = candidates[i];
            break;
        }

        preferredScreenPos = ClampDialogueToScreenBounds(preferredScreenPos, canvasMin, canvasMax, left, right, bottom, top);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, preferredScreenPos, _canvasCamera, out Vector2 localPosition))
            _dialogueRoot.anchoredPosition = localPosition;
    }

    /// <summary>
    /// 获取当前 Canvas 在屏幕坐标中的范围。
    /// </summary>
    private void GetCanvasScreenBounds(out Vector2 min, out Vector2 max)
    {
        if (_canvasRect == null)
        {
            min = Vector2.zero;
            max = new Vector2(Screen.width, Screen.height);
            return;
        }

        Vector3[] corners = new Vector3[4];
        _canvasRect.GetWorldCorners(corners);

        min = Vector2.positiveInfinity;
        max = Vector2.negativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(_canvasCamera, corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
    }

    /// <summary>
    /// 获取对话框 pivot 到四条边的屏幕空间距离。
    /// </summary>
    private void GetDialogueScreenExtents(out float left, out float right, out float bottom, out float top)
    {
        Vector2 size = GetDialogueScreenSize();
        Vector2 pivot = _dialogueRoot.pivot;

        left = size.x * pivot.x;
        right = size.x * (1f - pivot.x);
        bottom = size.y * pivot.y;
        top = size.y * (1f - pivot.y);
    }

    /// <summary>
    /// 获取对话框当前屏幕尺寸。
    /// </summary>
    private Vector2 GetDialogueScreenSize()
    {
        Vector3[] corners = new Vector3[4];
        _dialogueRoot.GetWorldCorners(corners);

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(_canvasCamera, corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return max - min;
    }

    /// <summary>
    /// 判断对话框放在指定 pivot 屏幕坐标时是否完整处于 Canvas 内。
    /// </summary>
    private bool IsDialogueInsideScreenBounds(
        Vector2 pivotPosition,
        Vector2 boundsMin,
        Vector2 boundsMax,
        float left,
        float right,
        float bottom,
        float top)
    {
        return pivotPosition.x - left >= boundsMin.x
            && pivotPosition.x + right <= boundsMax.x
            && pivotPosition.y - bottom >= boundsMin.y
            && pivotPosition.y + top <= boundsMax.y;
    }

    /// <summary>
    /// 将对话框 pivot 屏幕坐标限制在 Canvas 内，确保对话框不会越界。
    /// </summary>
    private Vector2 ClampDialogueToScreenBounds(
        Vector2 pivotPosition,
        Vector2 boundsMin,
        Vector2 boundsMax,
        float left,
        float right,
        float bottom,
        float top)
    {
        float minX = boundsMin.x + left;
        float maxX = boundsMax.x - right;
        float minY = boundsMin.y + bottom;
        float maxY = boundsMax.y - top;

        pivotPosition.x = minX <= maxX
            ? Mathf.Clamp(pivotPosition.x, minX, maxX)
            : (boundsMin.x + boundsMax.x) * 0.5f;
        pivotPosition.y = minY <= maxY
            ? Mathf.Clamp(pivotPosition.y, minY, maxY)
            : (boundsMin.y + boundsMax.y) * 0.5f;

        return pivotPosition;
    }

    /// <summary>
    /// 启动当前文本的打字机效果。
    /// </summary>
    private void BeginTypingCurrentText()
    {
        StopTyping();

        if (_dialogueText == null)
            return;

        _dialogueText.text = string.Empty;
        _typeCoroutine = StartCoroutine(TypeText(_currentFullText));
    }

    /// <summary>
    /// 逐字显示文本，使用真实时间以兼容游戏暂停。
    /// </summary>
    private IEnumerator TypeText(string fullText)
    {
        if (_dialogueText == null)
            yield break;

        _isTyping = true;
        _isFullTextShown = false;

        int length = fullText?.Length ?? 0;
        for (int i = 0; i < length; i++)
        {
            _dialogueText.text += fullText[i];
            yield return new WaitForSecondsRealtime(_typeCharDelay);
        }

        _isTyping = false;
        _isFullTextShown = true;
        _typeCoroutine = null;
    }

    /// <summary>
    /// 停止打字机协程。
    /// </summary>
    private void StopTyping()
    {
        if (_typeCoroutine == null)
            return;

        StopCoroutine(_typeCoroutine);
        _typeCoroutine = null;
    }

    /// <summary>
    /// 立即显示当前完整文本。
    /// </summary>
    private void ShowFullCurrentText()
    {
        StopTyping();

        if (_dialogueText != null)
            _dialogueText.text = _currentFullText;

        _isTyping = false;
        _isFullTextShown = true;
    }

    /// <summary>
    /// 设置 CanvasGroup 的显隐与交互状态。
    /// </summary>
    private void SetGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}

/// <summary>
/// 教学遮罩射线过滤器，根据 TutorialGuideUI 当前交互模式决定是否拦截点击。
/// </summary>
public class TutorialMaskRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    /// <summary>所属教学 UI。</summary>
    private TutorialGuideUI _owner;

    /// <summary>
    /// 初始化射线过滤器所属教学 UI。
    /// </summary>
    public void Initialize(TutorialGuideUI owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// 判断遮罩是否接收当前射线。
    /// </summary>
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        return _owner == null || _owner.ShouldMaskReceiveRaycast(screenPoint);
    }
}
