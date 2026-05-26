using System;
using System.Collections;
using System.Collections.Generic;
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

        [Header("提示框")]
        [SerializeField] [Tooltip("提示框根节点")]
        private RectTransform _tipRoot;

        [SerializeField] [Tooltip("提示框CanvasGroup")]
        private CanvasGroup _tipGroup;

        [SerializeField] [Tooltip("提示文本")]
        private TextMeshProUGUI _tipText;

        [SerializeField] [Tooltip("提示标题（可选）")]
        private TextMeshProUGUI _tipTitle;

        [SerializeField] [Tooltip("继续按钮/点击区域")]
        private Button _continueButton;

        [Header("动画设置")]
        [SerializeField] [Tooltip("淡入淡出时长")]
        private float _fadeDuration = 0.2f;

        [SerializeField] [Tooltip("提示框弹出缩放时长")]
        private float _popDuration = 0.25f;

        /// <summary>当前步骤完成回调</summary>
        private Action _onStepComplete;

        /// <summary>所在Canvas的RectTransform（用于边界计算）</summary>
        private RectTransform _canvasRect;

        /// <summary>Canvas 使用的摄像机（ScreenSpaceCamera 或 World）</summary>
        private Camera _canvasCamera;

        [Header("打字机设置")]
        [SerializeField] [Tooltip("每个字符间隔（秒），使用实时时间以兼容暂停）")]
        private float _typeCharDelay = 0.02f;

        private Coroutine _typeCoroutine;

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
        /// 根据 TutorialMark 显示引导（遮罩+高亮+TipPanel定位到旁边）
        /// </summary>
        public void ShowMark(TutorialMark mark, Action onComplete)
        {
            if (mark == null)
            {
                onComplete?.Invoke();
                return;
            }

            _onStepComplete = onComplete;

            // 将高亮区定位到目标
            RectTransform target = mark.HighlightRect;
            if (target != null)
            {
                PositionHighlightToTarget(target, mark.HighlightPadding);
            }

            // 显示遮罩
            ShowMask();

            // 设置提示内容
            if (_tipTitle != null)
            {
                bool hasTitle = !string.IsNullOrEmpty(mark.TipTitle);
                _tipTitle.gameObject.SetActive(hasTitle);
                if (hasTitle) _tipTitle.text = mark.TipTitle;
            }

            if (_tipText != null)
            {
                // 启动打字机效果（先清空文本）
                if (_typeCoroutine != null)
                    StopCoroutine(_typeCoroutine);
                _typeCoroutine = StartCoroutine(TypeText(mark.TipText));
            }

            // 显示TipPanel（文本会逐步填充并触发布局刷新）
            ShowTipPanel();
            PositionTipNearHighlight(mark.TipPositionMode, mark.TipOffset);

            // 继续按钮交互设置
            if (_continueButton != null)
                _continueButton.interactable = !mark.RequireClickTarget;
        }

        // ── 简易接口 ─────────────────────────────────────────

        /// <summary>显示纯文本提示（无高亮）</summary>
        public void ShowTip(string text)
        {
            if (_tipTitle != null)
                _tipTitle.gameObject.SetActive(false);

            if (_tipText != null)
            {
                if (_typeCoroutine != null)
                    StopCoroutine(_typeCoroutine);
                _typeCoroutine = StartCoroutine(TypeText(text));
            }

            // 显示 TipPanel
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
                SetGroupVisible(_tipGroup, false);

                if (_tipRoot != null)
                    _tipRoot.gameObject.SetActive(false);
            }
            else
            {
                HideMask();
                HideTipPanel();
            }

            _onStepComplete = null;


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
            if (_tipText == null) yield break;

            _tipText.text = string.Empty;
            int length = fullText?.Length ?? 0;

            for (int i = 0; i < length; i++)
            {
                _tipText.text += fullText[i];
                // 使用真实时间等待（暂停时仍能打字）
                yield return new WaitForSecondsRealtime(_typeCharDelay);

                // 每次添加字符后强制刷新文本布局，保证父Panel更新高度
                _tipText.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_tipText.rectTransform);
                if (_tipRoot != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_tipRoot);
            }

            _typeCoroutine = null;
        }

        /// <summary>将TipPanel定位到高亮区旁边</summary>
        private void PositionTipNearHighlight(TipPosition mode, float offset)
        {
            if (_tipRoot == null || _highlightArea == null) return;

            // TipPanel 自身大小（确保布局已计算）
            Vector2 tipSize = _tipRoot.sizeDelta;

            // 目标高亮在 tipRoot 父节点下的本地坐标计算
            RectTransform tipParent = _tipRoot.parent as RectTransform;
            if (tipParent == null) return;

            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;

            Vector3[] worldCorners = new Vector3[4];
            _highlightArea.GetWorldCorners(worldCorners);

            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_canvasCamera, worldCorners[i]);
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(tipParent, screenPoint, _canvasCamera, out localPoint);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            Vector2 highlightCenter = (min + max) * 0.5f;
            Vector2 highlightSize = (max - min);

            // Canvas边界（用于Auto模式判断空间和边界约束）
            Vector2 canvasSize = _canvasRect != null
                ? _canvasRect.sizeDelta
                : new Vector2(1920f, 1080f);
            float canvasHalfH = canvasSize.y * 0.5f;
            float canvasHalfW = canvasSize.x * 0.5f;

            // 确定最终位置
            TipPosition resolvedPos = mode;
            if (mode == TipPosition.Auto)
            {
                // 计算以 tipParent 坐标系为基准的空间判断：使用 highlightCenter.y
                float spaceBelow = canvasHalfH + (highlightCenter.y - highlightSize.y * 0.5f);
                resolvedPos = spaceBelow > tipSize.y + offset + 50f
                    ? TipPosition.Below
                    : TipPosition.Above;
            }

            Vector2 tipPos = highlightCenter;

            switch (resolvedPos)
            {
                case TipPosition.Above:
                    tipPos.y = highlightCenter.y + highlightSize.y * 0.5f + offset + tipSize.y * 0.5f;
                    break;
                case TipPosition.Below:
                    tipPos.y = highlightCenter.y - highlightSize.y * 0.5f - offset - tipSize.y * 0.5f;
                    break;
                case TipPosition.Left:
                    tipPos.x = highlightCenter.x - highlightSize.x * 0.5f - offset - tipSize.x * 0.5f;
                    break;
                case TipPosition.Right:
                    tipPos.x = highlightCenter.x + highlightSize.x * 0.5f + offset + tipSize.x * 0.5f;
                    break;
            }

            // 边界约束：以 tipParent 的 rect 为边界基准（若为 Canvas 则与之前一致）
            float halfTipW = tipSize.x * 0.5f;
            float halfTipH = tipSize.y * 0.5f;

            Rect parentRect = tipParent.rect;
            float parentHalfW = parentRect.width * 0.5f;
            float parentHalfH = parentRect.height * 0.5f;

            tipPos.x = Mathf.Clamp(tipPos.x, -parentHalfW + halfTipW + 10f, parentHalfW - halfTipW - 10f);
            tipPos.y = Mathf.Clamp(tipPos.y, -parentHalfH + halfTipH + 10f, parentHalfH - halfTipH - 10f);

            _tipRoot.anchoredPosition = tipPos;
        }

        // ── 内部方法 ─────────────────────────────────────────

        private void OnContinueClicked()
        {
            var callback = _onStepComplete;
            _onStepComplete = null;

            // 停止打字机协程
            if (_typeCoroutine != null)
            {
                StopCoroutine(_typeCoroutine);
            }

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
            if (_tipRoot != null)
                _tipRoot.gameObject.SetActive(true);

            if (_tipGroup != null)
            {
                _tipGroup.DOKill();
                _tipGroup.alpha = 0f;
                _tipGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
            }

            if (_tipRoot != null)
            {
                _tipRoot.DOKill();
                _tipRoot.localScale = Vector3.one * 0.8f;
                _tipRoot.DOScale(Vector3.one, _popDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private void HideTipPanel()
        {
            if (_tipGroup != null)
            {
                _tipGroup.DOKill();
                _tipGroup.DOFade(0f, _fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    if (_tipRoot != null)
                        _tipRoot.gameObject.SetActive(false);
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

        private void SetGroupVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
