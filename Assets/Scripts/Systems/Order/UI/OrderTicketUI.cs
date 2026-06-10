using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 订单小票UI组件
    /// 预览状态：显示静态缩略图贴图，点击后交叉淡入淡出切换到详细小票
    /// 详细状态：居中放大，可上下拖拽，收起时交叉淡回缩略图
    /// Q键：按住展开，松开收起
    /// </summary>
    public class OrderTicketUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("需求容器")]
        [SerializeField] [Tooltip("咖啡液需求容器")]
        private Transform _coffeeRequirementsContainer;

        [SerializeField] [Tooltip("辅助液需求容器")]
        private Transform _liquidRequirementsContainer;

        [SerializeField] [Tooltip("小料需求容器")]
        private Transform _toppingRequirementsContainer;

        [Header("预制体")]
        [SerializeField] [Tooltip("咖啡液需求项预制体")]
        private GameObject _coffeeRequirementItemPrefab;

        [SerializeField] [Tooltip("辅助液需求项预制体")]
        private GameObject _liquidRequirementItemPrefab;

        [SerializeField] [Tooltip("小料需求项预制体")]
        private GameObject _toppingRequirementItemPrefab;

        [Header("缩略图")]
        [SerializeField] [Tooltip("缩略图贴图的 CanvasGroup（预览状态显示）")]
        private CanvasGroup _thumbnailCanvasGroup;

        [SerializeField] [Tooltip("缩略图按钮（点击展开小票）")]
        private Button _thumbnailButton;

        [Header("详细小票")]
        [SerializeField] [Tooltip("详细小票内容的 CanvasGroup（展开状态显示）")]
        private CanvasGroup _detailCanvasGroup;

        [SerializeField] [Tooltip("详细小票的 RectTransform")]
        private RectTransform _detailRect;

        [SerializeField] [Tooltip("详细状态的锚点位置（屏幕中央）")]
        private Vector2 _detailAnchoredPos = Vector2.zero;

        [SerializeField] [Tooltip("详细状态的缩放比例")]
        private float _detailScale = 1f;

        [SerializeField] [Tooltip("收起状态的缩放比例")]
        private float _collapseScale = 0.7f;

        [Header("动画配置")]
        [SerializeField] [Tooltip("展开/收起动画时长")]
        private float _expandDuration = 0.35f;

        [SerializeField] [Tooltip("交叉淡入淡出时长（建议 <= expandDuration）")]
        private float _crossFadeDuration = 0.2f;

        [SerializeField] [Tooltip("展开动画曲线")]
        private Ease _expandEase = Ease.OutBack;

        [SerializeField] [Tooltip("收起动画曲线")]
        private Ease _collapseEase = Ease.InBack;

        [SerializeField] [Tooltip("展开后自动收起的延迟时长（秒）")]
        private float _autoCollapseDelay = 0.5f;

        [Header("收起位置")]
        [SerializeField] [Tooltip("收起后详细小票停放的锚点位置（屏幕外或左上角）")]
        private Vector2 _hiddenAnchoredPos = new Vector2(-300f, 300f);

        [Header("拖拽配置")]
        [SerializeField] [Tooltip("拖拽判定阈值（像素），超过此距离才算拖拽，否则算点击")]
        private float _dragThreshold = 10f;

        [SerializeField] [Tooltip("拖拽上边界（距屏幕顶部的最小距离，正值）")]
        private float _dragBoundTop = 50f;

        [SerializeField] [Tooltip("拖拽下边界（距屏幕底部的最小距离，正值）")]
        private float _dragBoundBottom = 50f;

        [Header("关闭按钮")]
        [SerializeField] [Tooltip("详细小票上的关闭/收起按钮")]
        private Button _detailCloseButton;

        /// <summary>缩略图淡入完成时触发（首次收起到预览状态）</summary>
        public event System.Action OnThumbnailShown;

        /// <summary>详细小票展开完成时触发。</summary>
        public event System.Action OnDetailShown;

        private RectTransform _canvasRect;
        private bool _isExpanded = false;
        private bool _isAnimating = false;
        private bool _pendingAutoCollapse = false;

        // 拖拽状态
        private bool _isDragging = false;
        private Vector2 _dragStartPointerPos;
        private Vector2 _dragStartAnchoredPos;

        private void Awake()
        {
            _thumbnailButton?.onClick.AddListener(OnThumbnailClicked);
            _detailCloseButton?.onClick.AddListener(OnDetailCloseClicked);

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _canvasRect = canvas.GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplyPreviewStateImmediate();
        }

        // ── 点击回调 ──────────────────────────────────────────

        private void OnThumbnailClicked()
        {
            if (_isAnimating || _isDragging) return;
            _pendingAutoCollapse = false;
            ExpandToDetailInternal();
        }

        private void OnDetailCloseClicked()
        {
            if (_isAnimating || _isDragging) return;
            CollapseToPreview();
        }

        // ── 拖拽接口 ──────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isExpanded || _isAnimating) return;

            _dragStartPointerPos  = eventData.position;
            _dragStartAnchoredPos = _detailRect.anchoredPosition;
            _isDragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isExpanded || _isAnimating) return;

            Vector2 delta = eventData.position - _dragStartPointerPos;

            if (!_isDragging && Mathf.Abs(delta.y) > _dragThreshold)
                _isDragging = true;

            if (!_isDragging) return;

            _detailRect.DOKill();

            float newY = _dragStartAnchoredPos.y + delta.y;

            float halfCanvasH = _canvasRect != null ? _canvasRect.rect.height * 0.5f : Screen.height * 0.5f;
            float halfTicketH = _detailRect.rect.height * _detailScale * 0.5f;

            float minY = -halfCanvasH + halfTicketH + _dragBoundBottom;
            float maxY =  halfCanvasH - halfTicketH - _dragBoundTop;

            newY = Mathf.Clamp(newY, minY, maxY);
            _detailRect.anchoredPosition = new Vector2(_detailRect.anchoredPosition.x, newY);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isDragging)
                StartCoroutine(ClearDragFlagNextFrame());
        }

        private IEnumerator ClearDragFlagNextFrame()
        {
            yield return null;
            _isDragging = false;
        }

        // ── 展开/收起 ─────────────────────────────────────────

        /// <summary>
        /// 展开为详细小票：缩略图淡出，详细小票淡入并放大居中
        /// </summary>
        public void ExpandToDetail()
        {
            _pendingAutoCollapse = false; // 任何外部调用都取消自动收起
            ExpandToDetailInternal();
        }

        private void ExpandToDetailInternal()
        {
            if (_isExpanded) return;
            _isExpanded  = true;
            _isAnimating = true;

            // 缩略图淡出
            _thumbnailCanvasGroup?.DOKill();
            _thumbnailCanvasGroup?.DOFade(0f, _crossFadeDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    //if (_thumbnailCanvasGroup != null)
                    //    _thumbnailCanvasGroup.blocksRaycasts = false;
                });

            // 详细小票从隐藏位置移到中央并淡入
            if (_detailCanvasGroup != null)
            {
                _detailCanvasGroup.DOKill();
                _detailCanvasGroup.alpha = 0f;
                _detailCanvasGroup.blocksRaycasts = true;
            }

            if (_detailRect != null)
            {
                _detailRect.DOKill();
                _detailRect.anchoredPosition = _hiddenAnchoredPos;
                _detailRect.localScale = Vector3.one * _collapseScale;

                Sequence seq = DOTween.Sequence();
                seq.Append(_detailRect.DOAnchorPos(_detailAnchoredPos, _expandDuration).SetEase(_expandEase));
                seq.Join(_detailRect.DOScale(_detailScale, _expandDuration).SetEase(_expandEase));
                seq.Join(_detailCanvasGroup.DOFade(1f, _crossFadeDuration).SetEase(Ease.OutQuad));
                seq.OnComplete(() =>
                {
                    _isAnimating = false;
                    OnDetailShown?.Invoke();
                });
            }
            else
            {
                _isAnimating = false;
            }
        }

        /// <summary>
        /// 收起为缩略图：详细小票淡出，缩略图淡入
        /// </summary>
        public void CollapseToPreview()
        {
            if (!_isExpanded) return;
            _isExpanded  = false;
            _isAnimating = true;

            // 详细小票淡出并缩小，两个动画时长一致避免布局重建闪白
            _detailCanvasGroup?.DOKill();
            _detailRect?.DOKill();

            if (_detailCanvasGroup != null)
                _detailCanvasGroup.alpha = 1f;

            Sequence collapseSeq = DOTween.Sequence();
            if (_detailCanvasGroup != null)
                collapseSeq.Join(_detailCanvasGroup.DOFade(0f, _expandDuration).SetEase(Ease.InQuad));
            if (_detailRect != null)
                collapseSeq.Join(_detailRect.DOScale(_collapseScale, _expandDuration).SetEase(_collapseEase));
            collapseSeq.OnComplete(() =>
            {
                if (_detailCanvasGroup != null)
                    _detailCanvasGroup.blocksRaycasts = false;
                if (_detailRect != null)
                    _detailRect.anchoredPosition = _hiddenAnchoredPos;
            });

            // 缩略图淡入
            if (_thumbnailCanvasGroup != null)
            {
                _thumbnailCanvasGroup.DOKill();
                _thumbnailCanvasGroup.blocksRaycasts = true;
                _thumbnailCanvasGroup.DOFade(1f, _crossFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        _isAnimating = false;
                        OnThumbnailShown?.Invoke();
                    });
            }
            else
            {
                _isAnimating = false;
            }
        }

        // ── 刷新小票 ──────────────────────────────────────────

        /// <summary>
        /// 刷新订单小票显示，刷新完毕后自动展开详细状态
        /// </summary>
        public void RefreshTicket(OrderSO order)
        {
            if (order == null)
            {
                ClearTicket();
                return;
            }

            // 生成子条目前确保 detailCanvasGroup 完全透明，防止布局重建期间闪白
            if (_detailCanvasGroup != null)
                _detailCanvasGroup.alpha = 0f;

            DisplayCoffeeRequirements(order);
            DisplayLiquidRequirements(order);
            DisplayToppingRequirements(order);

            StartCoroutine(RefreshLayoutThenExpand());
        }

        private IEnumerator RefreshLayoutThenExpand()
        {
            // 等两帧：第一帧 Instantiate 完成，第二帧 ContentSizeFitter 完成布局重建
            yield return null;
            yield return null;
            ForceRebuildLayout();
            ExpandToDetailInternal(); // 系统触发，不取消自动收起标记

            // 系统生成时自动收起
            _pendingAutoCollapse = true;
            yield return new WaitForSeconds(_expandDuration + _autoCollapseDelay);
            yield return TutorialGate.WaitForRelease(TutorialGateKey.BeforeFirstTicketAutoCollapse);
            if (_pendingAutoCollapse && _isExpanded && !_isAnimating)
                CollapseToPreview();
            _pendingAutoCollapse = false;
        }

        private void ForceRebuildLayout()
        {
            if (_coffeeRequirementsContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_coffeeRequirementsContainer as RectTransform);
            if (_liquidRequirementsContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_liquidRequirementsContainer as RectTransform);
            if (_toppingRequirementsContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_toppingRequirementsContainer as RectTransform);

            if (_detailRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_detailRect);
        }

        private void DisplayCoffeeRequirements(OrderSO order)
        {
            ClearContainer(_coffeeRequirementsContainer);
            if (order.coffeeRequirements == null) return;

            foreach (var requirement in order.coffeeRequirements)
            {
                if (requirement.bean == null) continue;
                var item = Instantiate(_coffeeRequirementItemPrefab, _coffeeRequirementsContainer);
                item.GetComponent<CoffeeRequirementItemUI>().Init(requirement);
            }
        }

        private void DisplayLiquidRequirements(OrderSO order)
        {
            ClearContainer(_liquidRequirementsContainer);
            if (order.liquidRequirements == null) return;

            foreach (var requirement in order.liquidRequirements)
            {
                if (requirement.liquid == null) continue;
                var item = Instantiate(_liquidRequirementItemPrefab, _liquidRequirementsContainer);
                item.GetComponent<LiquidRequirementItemUI>().Init(requirement);
            }
        }

        private void DisplayToppingRequirements(OrderSO order)
        {
            ClearContainer(_toppingRequirementsContainer);
            if (order.toppingRequirement == null || order.toppingRequirement.requiredToppings == null) return;

            foreach (var topping in order.toppingRequirement.requiredToppings)
            {
                if (topping == null) continue;
                var item = Instantiate(_toppingRequirementItemPrefab, _toppingRequirementsContainer);
                item.GetComponent<ToppingRequirementItemUI>().Init(topping);
            }
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        public void ClearTicket()
        {
            ClearContainer(_coffeeRequirementsContainer);
            ClearContainer(_liquidRequirementsContainer);
            ClearContainer(_toppingRequirementsContainer);
        }

        // ── 重置 ──────────────────────────────────────────────

        /// <summary>
        /// 重置小票状态（新一单或提交后调用）
        /// </summary>
        public void ResetTicketState()
        {
            StopAllCoroutines();

            _thumbnailCanvasGroup?.DOKill();
            _detailCanvasGroup?.DOKill();
            _detailRect?.DOKill();

            _isAnimating = false;
            _isExpanded  = false;
            _isDragging  = false;

            ClearTicket();
            ApplyPreviewStateImmediate();
        }

        private void ApplyPreviewStateImmediate()
        {
            // 重置时缩略图也隐藏，等详细小票收起后才显示
            if (_thumbnailCanvasGroup != null)
            {
                _thumbnailCanvasGroup.alpha          = 0f;
                _thumbnailCanvasGroup.blocksRaycasts = false;
            }

            // 详细小票隐藏，移到屏幕外
            if (_detailCanvasGroup != null)
            {
                _detailCanvasGroup.alpha          = 0f;
                _detailCanvasGroup.blocksRaycasts = false;
            }

            if (_detailRect != null)
            {
                _detailRect.anchoredPosition = _hiddenAnchoredPos;
                _detailRect.localScale       = Vector3.one * _collapseScale;
            }
        }
    }
}
