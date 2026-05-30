using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 教学引导UI组件
    /// 全局唯一，负责控制高亮区域和TipPanel
    /// 遮罩通过Material实现反向遮罩（Stencil），代码只需控制高亮区Image的RectTransform
    /// 结构：引导UI(CanvasGroup) → HollowMask → 高亮区(Image) + 黑幕(Image) + TipPanel + ContinueButton
    /// </summary>
    public class TutorialGuideUI : MonoBehaviour
    {
        [Header("整体控制")]
        [SerializeField] [Tooltip("引导UI自身的CanvasGroup（控制整体显隐和交互拦截）")]
        private CanvasGroup _maskGroup;

        [Header("高亮区")]
        [SerializeField] [Tooltip("高亮区Image的RectTransform（调整位置和大小即可实现镂空）")]
        private RectTransform _highlightArea;

        [Header("老板对话框")]
        [SerializeField] [Tooltip("对话框根节点（位置由场景手动摆放）")]
        private RectTransform _dialogueRoot;

        [SerializeField] [Tooltip("对话框CanvasGroup")]
        private CanvasGroup _dialogueGroup;

        [SerializeField] [Tooltip("老板立绘")]
        private Image _bossPortrait;

        [SerializeField] [Tooltip("说话者名字")]
        private TextMeshProUGUI _speakerNameText;

        [SerializeField] [Tooltip("对话文本")]
        private TextMeshProUGUI _dialogueText;

        [SerializeField] [Tooltip("继续按钮")]
        private Button _continueButton;

        [Header("动画设置")]
        [SerializeField] [Tooltip("淡入淡出时长")]
        private float _fadeDuration = 0.2f;

        [SerializeField] [Tooltip("对话框弹出缩放时长")]
        private float _popDuration = 0.25f;

        [SerializeField] [Tooltip("聚光灯收缩动画时长（从全屏缩小到高亮区）")]
        private float _spotlightDuration = 0.4f;

        /// <summary>当前步骤完成回调</summary>
        private Action _onStepComplete;

        /// <summary>当前教学标记</summary>
        private TutorialMark _currentMark;

        /// <summary>当前句序号</summary>
        private int _currentDialogueIndex;

        /// <summary>所在Canvas的RectTransform（用于边界计算）</summary>
        private RectTransform _canvasRect;

        /// <summary>Canvas 使用的摄像机（ScreenSpaceCamera 或 World）</summary>
        private Camera _canvasCamera;

        [Header("对话框自动定位")]
        [SerializeField] [Tooltip("对话框与高亮框之间的间距（像素）")]
        private float _dialogueOffset = 20f;

        [Header("打字机设置")]
        [SerializeField] [Tooltip("每个字符间隔（秒），使用实时时间以兼容暂停）")]
        private float _typeCharDelay = 0.02f;

        private Coroutine _typeCoroutine;
        private string _currentFullText = string.Empty;
        private bool _isTyping = false;
        private bool _isFullTextShown = false;

        private void Awake()
        {
            HideAll(immediate: true);

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);

            // 缓存Canvas引用
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _canvasRect = canvas.GetComponent<RectTransform>();
                _canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            }
        }

        // ── TutorialMark 驱动接口（主要使用方式）─────────────

        /// <summary>
        /// 根据 TutorialMark 显示引导（遮罩+高亮+老板对话框）
        /// </summary>
        public void ShowMark(TutorialMark mark, Action onComplete)
        {
            if (mark == null)
            {
                onComplete?.Invoke();
                return;
            }

            _currentMark = mark;
            _currentDialogueIndex = 0;
            _onStepComplete = onComplete;

            // 将高亮区定位到目标（含聚光灯动画）
            RectTransform target = mark.HighlightRect;
            if (target != null)
            {
                ComputeHighlightRects(target, mark.HighlightPadding,
                    out Vector2 targetCenter, out Vector2 targetSize,
                    out Vector2 screenMin, out Vector2 screenMax);

                if (_highlightArea != null)
                {
                    // 初始设为全屏大小，制造聚光灯收缩效果
                    Vector2 fullSize = _canvasRect != null
                        ? _canvasRect.rect.size
                        : new Vector2(Screen.width, Screen.height);

                    _highlightArea.DOKill();
                    _highlightArea.anchoredPosition = Vector2.zero;
                    _highlightArea.sizeDelta = fullSize;

                    // 动画收缩到目标区域
                    _highlightArea.DOAnchorPos(targetCenter, _spotlightDuration)
                        .SetEase(Ease.OutCubic).SetUpdate(true);
                    _highlightArea.DOSizeDelta(targetSize, _spotlightDuration)
                        .SetEase(Ease.OutCubic).SetUpdate(true);
                }

                // 对话框定位基于最终目标位置（不受动画影响）
                if (_dialogueRoot != null && _canvasRect != null)
                    PositionDialogueNextToHighlight(screenMin, screenMax, mark.HighlightPadding);
            }

            // 显示遮罩
            ShowMask();

            // 固定说话者名字
            if (_speakerNameText != null)
            {
                _speakerNameText.text = "店老板";
                _speakerNameText.gameObject.SetActive(true);
            }

            // 显示对话框（位置已由 PositionDialogueNextToHighlight 自动计算）
            ShowTipPanel();

            PlayCurrentDialogue();
        }

        // ── 简易接口 ─────────────────────────────────────────

        /// <summary>显示纯文本提示（无高亮）</summary>
        public void ShowTip(string text)
        {
            _currentMark = null;
            _currentDialogueIndex = 0;
            _currentFullText = text ?? string.Empty;
            _isTyping = false;
            _isFullTextShown = false;

            if (_speakerNameText != null)
            {
                _speakerNameText.text = "店老板";
                _speakerNameText.gameObject.SetActive(true);
            }

            ApplyPortrait(null);
            BeginTypingCurrentText();

            // 显示对话框
            ShowTipPanel();
        }

        /// <summary>隐藏提示</summary>
        public void HideTip()
        {
            HideTipPanel();
        }

        /// <summary>显示高亮覆盖指定目标</summary>
        public void ShowHighlight(RectTransform target, float padding = 20f)
        {
            if (target == null) return;

            PositionHighlightToTarget(target, padding);
            ShowMask();
        }

        /// <summary>隐藏高亮</summary>
        public void HideHighlight()
        {
            HideMask();
        }

        /// <summary>完成当前步骤（供外部调用，如自动推进）</summary>
        public void CompleteCurrentStep()
        {
            OnContinueClicked();
        }

        /// <summary>隐藏所有引导元素</summary>
        public void HideAll(bool immediate = false)
        {
            if (immediate)
            {
                if (_maskGroup != null)
                {
                    _maskGroup.alpha = 0f;
                    _maskGroup.interactable = false;
                    _maskGroup.blocksRaycasts = false;
                }
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
            _currentMark = null;
            _currentDialogueIndex = 0;
            _currentFullText = string.Empty;
            _isTyping = false;
            _isFullTextShown = false;

            // 停止打字机协程（如果存在）
            if (_typeCoroutine != null)
            {
                StopCoroutine(_typeCoroutine);
                _typeCoroutine = null;
            }
        }

        // ── TipPanel 定位逻辑 ────────────────────────────────

        private IEnumerator TypeText(string fullText)
        {
            if (_dialogueText == null) yield break;

            _isTyping = true;
            _isFullTextShown = false;
            _dialogueText.text = string.Empty;
            int length = fullText?.Length ?? 0;

            for (int i = 0; i < length; i++)
            {
                _dialogueText.text += fullText[i];
                // 使用真实时间等待（暂停时仍能打字）
                yield return new WaitForSecondsRealtime(_typeCharDelay);

                _dialogueText.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_dialogueText.rectTransform);
                if (_dialogueRoot != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_dialogueRoot);
            }

            _isTyping = false;
            _isFullTextShown = true;
            _typeCoroutine = null;
            if (_continueButton != null)
                _continueButton.interactable = true;
        }

        // ── 内部方法 ─────────────────────────────────────────

        private void OnContinueClicked()
        {
            if (_isTyping)
            {
                ShowFullCurrentText();
                return;
            }

            if (!_isFullTextShown)
            {
                ShowFullCurrentText();
                return;
            }

            if (_currentMark != null && _currentDialogueIndex + 1 < _currentMark.GetDialogueCount())
            {
                _currentDialogueIndex++;
                PlayCurrentDialogue();
                return;
            }

            var callback = _onStepComplete;
            _onStepComplete = null;

            HideAll();
            callback?.Invoke();
        }

        private void ShowMask()
        {
            if (_maskGroup != null)
            {
                _maskGroup.DOKill();
                _maskGroup.alpha = 0f;
                _maskGroup.interactable = true;
                _maskGroup.blocksRaycasts = true;
                _maskGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
            }
        }

        private void HideMask()
        {
            if (_maskGroup != null)
            {
                _maskGroup.DOKill();
                _maskGroup.DOFade(0f, _fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    _maskGroup.interactable = false;
                    _maskGroup.blocksRaycasts = false;
                });
            }
        }

        private void ShowTipPanel()
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

        private void HideTipPanel()
        {
            if (_dialogueGroup != null)
            {
                _dialogueGroup.DOKill();
                _dialogueGroup.DOFade(0f, _fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    if (_dialogueRoot != null)
                        _dialogueRoot.gameObject.SetActive(false);
                });
            }
        }

        /// <summary>将高亮区定位到目标RectTransform的位置和大小，并自动定位对话框（立即生效，无动画）</summary>
        private void PositionHighlightToTarget(RectTransform target, float padding)
        {
            if (_highlightArea == null || target == null) return;

            ComputeHighlightRects(target, padding,
                out Vector2 center, out Vector2 size,
                out Vector2 screenMin, out Vector2 screenMax);

            _highlightArea.anchoredPosition = center;
            _highlightArea.sizeDelta        = size;

            if (_dialogueRoot != null && _canvasRect != null)
                PositionDialogueNextToHighlight(screenMin, screenMax, padding);
        }

        /// <summary>
        /// 计算目标RectTransform对应的高亮区本地坐标（center/size）和屏幕坐标（screenMin/screenMax）
        /// </summary>
        private void ComputeHighlightRects(RectTransform target, float padding,
            out Vector2 center, out Vector2 size,
            out Vector2 screenMin, out Vector2 screenMax)
        {
            center    = Vector2.zero;
            size      = Vector2.zero;
            screenMin = Vector2.positiveInfinity;
            screenMax = Vector2.negativeInfinity;

            if (_highlightArea == null || target == null) return;

            RectTransform parent = _highlightArea.parent as RectTransform;
            if (parent == null) return;

            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);

            Vector2 localMin = Vector2.positiveInfinity;
            Vector2 localMax = Vector2.negativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_canvasCamera, worldCorners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, _canvasCamera, out Vector2 localPoint);

                localMin  = Vector2.Min(localMin,  localPoint);
                localMax  = Vector2.Max(localMax,  localPoint);
                screenMin = Vector2.Min(screenMin, screenPoint);
                screenMax = Vector2.Max(screenMax, screenPoint);
            }

            center = (localMin + localMax) * 0.5f;
            size   = (localMax - localMin) + Vector2.one * padding * 2f;
        }

        /// <summary>
        /// 根据高亮框屏幕坐标，自动选择上下左右能完整容纳对话框的方向放置对话框。
        /// 优先顺序：下 → 上 → 右 → 左，若某方向放不下则跳过，全部放不下时选剩余空间最大的方向并强制夹紧。
        /// </summary>
        private void PositionDialogueNextToHighlight(Vector2 screenMin, Vector2 screenMax, float padding)
        {
            RectTransform dialogParent = _dialogueRoot.parent as RectTransform;
            if (dialogParent == null) return;

            // 高亮框四条边（含 padding）的屏幕坐标
            float sLeft   = screenMin.x - padding;
            float sRight  = screenMax.x + padding;
            float sBottom = screenMin.y - padding;
            float sTop    = screenMax.y + padding;

            // 高亮框中心屏幕坐标
            Vector2 screenCenter = new Vector2((sLeft + sRight) * 0.5f, (sBottom + sTop) * 0.5f);

            // 对话框尺寸
            Vector2 dialogSize = _dialogueRoot.rect.size;
            float   dw         = dialogSize.x;
            float   dh         = dialogSize.y;
            float   halfDW     = dw * 0.5f;
            float   halfDH     = dh * 0.5f;

            float sw = Screen.width;
            float sh = Screen.height;

            // 各方向放置后对话框中心的屏幕坐标（未夹紧）
            // 下：对话框顶边紧贴高亮框底边
            Vector2 posDown  = new Vector2(screenCenter.x, sBottom - _dialogueOffset - halfDH);
            // 上：对话框底边紧贴高亮框顶边
            Vector2 posUp    = new Vector2(screenCenter.x, sTop    + _dialogueOffset + halfDH);
            // 右：对话框左边紧贴高亮框右边
            Vector2 posRight = new Vector2(sRight + _dialogueOffset + halfDW, screenCenter.y);
            // 左：对话框右边紧贴高亮框左边
            Vector2 posLeft  = new Vector2(sLeft  - _dialogueOffset - halfDW, screenCenter.y);

            // 判断某个中心点放置对话框后是否完全在屏幕内
            bool FitsOnScreen(Vector2 center)
            {
                return center.x - halfDW >= 0f && center.x + halfDW <= sw
                    && center.y - halfDH >= 0f && center.y + halfDH <= sh;
            }

            // 按优先顺序选第一个能放下的方向
            Vector2[] candidates = { posDown, posUp, posRight, posLeft };
            Vector2 chosen = Vector2.zero;
            bool found = false;
            foreach (var c in candidates)
            {
                if (FitsOnScreen(c)) { chosen = c; found = true; break; }
            }

            if (!found)
            {
                // 全部放不下：选剩余空间最大的方向，然后强制夹紧到屏幕内
                float spaceDown  = sBottom;
                float spaceUp    = sh - sTop;
                float spaceRight = sw - sRight;
                float spaceLeft  = sLeft;
                float best = Mathf.Max(spaceDown, spaceUp, spaceRight, spaceLeft);

                if      (best == spaceDown)  chosen = posDown;
                else if (best == spaceUp)    chosen = posUp;
                else if (best == spaceRight) chosen = posRight;
                else                         chosen = posLeft;

                // 夹紧，确保不超出屏幕
                chosen.x = Mathf.Clamp(chosen.x, halfDW, sw - halfDW);
                chosen.y = Mathf.Clamp(chosen.y, halfDH, sh - halfDH);
            }

            // 屏幕坐标转换到 _dialogueRoot 父节点的本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dialogParent, chosen, _canvasCamera, out Vector2 localPos);

            _dialogueRoot.anchoredPosition = localPos;
        }

        private void PlayCurrentDialogue()
        {
            if (_currentMark == null)
            {
                CompleteSequence();
                return;
            }

            int count = _currentMark.GetDialogueCount();
            if (count <= 0)
            {
                CompleteSequence();
                return;
            }

            if (_currentDialogueIndex < 0 || _currentDialogueIndex >= count)
            {
                _currentDialogueIndex = 0;
            }

            _currentFullText = _currentMark.GetDialogueText(_currentDialogueIndex) ?? string.Empty;
            _isTyping = false;
            _isFullTextShown = false;

            ApplyPortrait(_currentMark.GetDialoguePortrait(_currentDialogueIndex));
            BeginTypingCurrentText();
        }

        private void BeginTypingCurrentText()
        {
            if (_dialogueText == null)
                return;

            if (_typeCoroutine != null)
            {
                StopCoroutine(_typeCoroutine);
                _typeCoroutine = null;
            }

            _dialogueText.text = string.Empty;
            _typeCoroutine = StartCoroutine(TypeText(_currentFullText));
        }

        private void ShowFullCurrentText()
        {
            if (_typeCoroutine != null)
            {
                StopCoroutine(_typeCoroutine);
                _typeCoroutine = null;
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = _currentFullText;
            }

            _isTyping = false;
            _isFullTextShown = true;
            if (_continueButton != null)
                _continueButton.interactable = true;
        }

        private void ApplyPortrait(Sprite portrait)
        {
            if (_bossPortrait == null)
                return;

            bool hasPortrait = portrait != null;
            _bossPortrait.gameObject.SetActive(hasPortrait);
            if (hasPortrait)
            {
                _bossPortrait.sprite = portrait;
            }
        }

        private void CompleteSequence()
        {
            var callback = _onStepComplete;
            _onStepComplete = null;
            HideAll();
            callback?.Invoke();
        }

        private void SetGroupVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
