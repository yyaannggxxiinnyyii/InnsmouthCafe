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

            // 将高亮区定位到目标
            RectTransform target = mark.HighlightRect;
            if (target != null)
            {
                PositionHighlightToTarget(target, mark.HighlightPadding);
            }

            // 显示遮罩
            ShowMask();

            // 固定说话者名字
            if (_speakerNameText != null)
            {
                _speakerNameText.text = "店老板";
                _speakerNameText.gameObject.SetActive(true);
            }

            // 显示对话框（位置由场景手动摆放）
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

        /// <summary>将高亮区定位到目标RectTransform的位置和大小</summary>
        private void PositionHighlightToTarget(RectTransform target, float padding)
        {
            if (_highlightArea == null || target == null) return;

            // 获取目标世界坐标四角
            Vector3[] worldCorners = new Vector3[4];
            target.GetWorldCorners(worldCorners);

            // 转换到高亮区父节点（HollowMask）的本地坐标系
            RectTransform parent = _highlightArea.parent as RectTransform;
            if (parent == null) return;

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_canvasCamera, worldCorners[i]);
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, _canvasCamera, out localPoint);

                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            Vector2 center = (min + max) * 0.5f;
            Vector2 size = (max - min) + Vector2.one * padding * 2f;

            _highlightArea.anchoredPosition = center;
            _highlightArea.sizeDelta = size;
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
