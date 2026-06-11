using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 订单小票详情面板UI，负责展示单张订单的完整需求与展开、收起、拖拽逻辑。
    /// </summary>
    public class OrderTicketDetailPanelUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("需求容器")]
        [SerializeField]
        [Tooltip("咖啡液需求容器")]
        private Transform _coffeeRequirementsContainer;

        [SerializeField]
        [Tooltip("辅助液需求容器")]
        private Transform _liquidRequirementsContainer;

        [SerializeField]
        [Tooltip("小料需求容器")]
        private Transform _toppingRequirementsContainer;

        [Header("预制体")]
        [SerializeField]
        [Tooltip("咖啡液需求项预制体")]
        private GameObject _coffeeRequirementItemPrefab;

        [SerializeField]
        [Tooltip("辅助液需求项预制体")]
        private GameObject _liquidRequirementItemPrefab;

        [SerializeField]
        [Tooltip("小料需求项预制体")]
        private GameObject _toppingRequirementItemPrefab;

        [Header("详细小票")]
        [SerializeField]
        [Tooltip("详细小票内容的 CanvasGroup")]
        private CanvasGroup _detailCanvasGroup;

        [SerializeField]
        [Tooltip("详细小票的 RectTransform")]
        private RectTransform _detailRect;

        [SerializeField]
        [Tooltip("详细状态的锚点位置（屏幕中央）")]
        private Vector2 _detailAnchoredPos = Vector2.zero;

        [SerializeField]
        [Tooltip("详细状态的缩放比例")]
        private float _detailScale = 1f;

        [SerializeField]
        [Tooltip("收起状态的缩放比例")]
        private float _collapseScale = 0.7f;

        [Header("动画配置")]
        [SerializeField]
        [Tooltip("展开/收起动画时长")]
        private float _expandDuration = 0.35f;

        [SerializeField]
        [Tooltip("交叉淡入淡出时长（建议 <= expandDuration）")]
        private float _crossFadeDuration = 0.2f;

        [SerializeField]
        [Tooltip("展开动画曲线")]
        private Ease _expandEase = Ease.OutBack;

        [SerializeField]
        [Tooltip("收起动画曲线")]
        private Ease _collapseEase = Ease.InBack;

        [SerializeField]
        [Tooltip("展开后自动收起的延迟时长（秒）")]
        private float _autoCollapseDelay = 0.5f;

        [Header("收起位置")]
        [SerializeField]
        [Tooltip("收起后详细小票停放的锚点位置")]
        private Vector2 _hiddenAnchoredPos = new Vector2(-300f, 300f);

        [Header("拖拽配置")]
        [SerializeField]
        [Tooltip("拖拽判定阈值（像素），超过此距离才算拖拽")]
        private float _dragThreshold = 10f;

        [SerializeField]
        [Tooltip("拖拽上边界（距屏幕顶部的最小距离，正值）")]
        private float _dragBoundTop = 50f;

        [SerializeField]
        [Tooltip("拖拽下边界（距屏幕底部的最小距离，正值）")]
        private float _dragBoundBottom = 50f;

        [Header("关闭按钮")]
        [SerializeField]
        [Tooltip("详细小票上的关闭/收起按钮")]
        private Button _closeButton;

        /// <summary>
        /// 详情面板展开完成时触发。
        /// </summary>
        public event Action OnDetailShown;

        /// <summary>
        /// 详情面板收起完成时触发。
        /// </summary>
        public event Action OnDetailHidden;

        /// <summary>
        /// 当前绑定的订单槽。
        /// </summary>
        public CustomerOrderSlotData BoundSlot { get; private set; }

        /// <summary>
        /// 当前绑定的订单配置。
        /// </summary>
        public OrderSO BoundOrder { get; private set; }

        private RectTransform _canvasRect;
        private bool _isExpanded;
        private bool _isAnimating;
        private bool _pendingAutoCollapse;
        private bool _isDragging;
        private Vector2 _dragStartPointerPos;
        private Vector2 _dragStartAnchoredPos;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Collapse);

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _canvasRect = canvas.GetComponent<RectTransform>();
            }
        }

        private void Start()
        {
            HideImmediate();
        }

        private void OnDestroy()
        {
            _closeButton?.onClick.RemoveListener(Collapse);
        }

        /// <summary>
        /// 展示指定订单槽详情。
        /// </summary>
        /// <param name="slot">需要展示的订单槽。</param>
        /// <param name="autoCollapse">是否在展开后自动收起。</param>
        public void ShowSlot(CustomerOrderSlotData slot, bool autoCollapse)
        {
            BoundSlot = slot;
            ShowOrder(slot?.orderSO, autoCollapse);
        }

        /// <summary>
        /// 展示指定订单详情。
        /// </summary>
        /// <param name="order">需要展示的订单配置。</param>
        /// <param name="autoCollapse">是否在展开后自动收起。</param>
        public void ShowOrder(OrderSO order, bool autoCollapse)
        {
            if (order == null)
            {
                ResetDetail();
                return;
            }

            BoundOrder = order;
            StopAllCoroutines();
            ClearDetail();
            DisplayCoffeeRequirements(order);
            DisplayLiquidRequirements(order);
            DisplayToppingRequirements(order);
            StartCoroutine(RefreshLayoutThenExpand(autoCollapse));
        }

        /// <summary>
        /// 收起详情面板。
        /// </summary>
        public void Collapse()
        {
            if (!_isExpanded || _isAnimating)
            {
                return;
            }

            _pendingAutoCollapse = false;
            _isExpanded = false;
            _isAnimating = true;

            _detailCanvasGroup?.DOKill();
            _detailRect?.DOKill();

            if (_detailCanvasGroup != null)
            {
                _detailCanvasGroup.alpha = 1f;
            }

            Sequence collapseSequence = DOTween.Sequence();
            if (_detailCanvasGroup != null)
            {
                collapseSequence.Join(_detailCanvasGroup.DOFade(0f, _expandDuration).SetEase(Ease.InQuad));
            }

            if (_detailRect != null)
            {
                collapseSequence.Join(_detailRect.DOScale(_collapseScale, _expandDuration).SetEase(_collapseEase));
            }

            collapseSequence.OnComplete(() =>
            {
                _isAnimating = false;
                HideImmediate();
                OnDetailHidden?.Invoke();
            });
        }

        /// <summary>
        /// 清空详情内容并立即隐藏面板。
        /// </summary>
        public void ResetDetail()
        {
            StopAllCoroutines();
            BoundSlot = null;
            BoundOrder = null;
            ClearDetail();
            HideImmediate();
        }

        /// <summary>
        /// 开始拖拽详情面板。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isExpanded || _isAnimating || _detailRect == null)
            {
                return;
            }

            _dragStartPointerPos = eventData.position;
            _dragStartAnchoredPos = _detailRect.anchoredPosition;
            _isDragging = false;
        }

        /// <summary>
        /// 拖拽详情面板。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isExpanded || _isAnimating || _detailRect == null)
            {
                return;
            }

            Vector2 delta = eventData.position - _dragStartPointerPos;
            if (!_isDragging && Mathf.Abs(delta.y) > _dragThreshold)
            {
                _isDragging = true;
            }

            if (!_isDragging)
            {
                return;
            }

            _detailRect.DOKill();

            float halfCanvasHeight = _canvasRect != null
                ? _canvasRect.rect.height * 0.5f
                : Screen.height * 0.5f;
            float halfTicketHeight = _detailRect.rect.height * _detailScale * 0.5f;
            float minY = -halfCanvasHeight + halfTicketHeight + _dragBoundBottom;
            float maxY = halfCanvasHeight - halfTicketHeight - _dragBoundTop;
            float newY = Mathf.Clamp(_dragStartAnchoredPos.y + delta.y, minY, maxY);

            _detailRect.anchoredPosition = new Vector2(_detailRect.anchoredPosition.x, newY);
        }

        /// <summary>
        /// 结束拖拽详情面板。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isDragging)
            {
                StartCoroutine(ClearDragFlagNextFrame());
            }
        }

        /// <summary>
        /// 下一帧清除拖拽标记，避免拖拽结束瞬间触发点击。
        /// </summary>
        private IEnumerator ClearDragFlagNextFrame()
        {
            yield return null;
            _isDragging = false;
        }

        /// <summary>
        /// 等待布局刷新后展开详情面板。
        /// </summary>
        /// <param name="autoCollapse">是否在展开后自动收起。</param>
        private IEnumerator RefreshLayoutThenExpand(bool autoCollapse)
        {
            yield return null;
            yield return null;
            ForceRebuildLayout();
            Expand();

            if (!autoCollapse)
            {
                _pendingAutoCollapse = false;
                yield break;
            }

            _pendingAutoCollapse = true;
            yield return new WaitForSeconds(_expandDuration + _autoCollapseDelay);
            yield return TutorialGate.WaitForRelease(TutorialGateKey.BeforeFirstTicketAutoCollapse);
            if (_pendingAutoCollapse && _isExpanded && !_isAnimating)
            {
                Collapse();
            }

            _pendingAutoCollapse = false;
        }

        /// <summary>
        /// 展开详情面板。
        /// </summary>
        private void Expand()
        {
            if (_isExpanded || _isAnimating)
            {
                return;
            }

            _isExpanded = true;
            _isAnimating = true;

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

                Sequence sequence = DOTween.Sequence();
                sequence.Append(_detailRect.DOAnchorPos(_detailAnchoredPos, _expandDuration).SetEase(_expandEase));
                sequence.Join(_detailRect.DOScale(_detailScale, _expandDuration).SetEase(_expandEase));
                if (_detailCanvasGroup != null)
                {
                    sequence.Join(_detailCanvasGroup.DOFade(1f, _crossFadeDuration).SetEase(Ease.OutQuad));
                }

                sequence.OnComplete(() =>
                {
                    _isAnimating = false;
                    OnDetailShown?.Invoke();
                });
            }
            else
            {
                _isAnimating = false;
                OnDetailShown?.Invoke();
            }
        }

        /// <summary>
        /// 立即隐藏详情面板。
        /// </summary>
        private void HideImmediate()
        {
            _detailCanvasGroup?.DOKill();
            _detailRect?.DOKill();
            _isExpanded = false;
            _isAnimating = false;
            _isDragging = false;

            if (_detailCanvasGroup != null)
            {
                _detailCanvasGroup.alpha = 0f;
                _detailCanvasGroup.blocksRaycasts = false;
            }

            if (_detailRect != null)
            {
                _detailRect.anchoredPosition = _hiddenAnchoredPos;
                _detailRect.localScale = Vector3.one * _collapseScale;
            }
        }

        /// <summary>
        /// 强制刷新详情布局。
        /// </summary>
        private void ForceRebuildLayout()
        {
            if (_coffeeRequirementsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_coffeeRequirementsContainer as RectTransform);
            }

            if (_liquidRequirementsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_liquidRequirementsContainer as RectTransform);
            }

            if (_toppingRequirementsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_toppingRequirementsContainer as RectTransform);
            }

            if (_detailRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_detailRect);
            }
        }

        /// <summary>
        /// 显示咖啡液需求列表。
        /// </summary>
        /// <param name="order">订单配置。</param>
        private void DisplayCoffeeRequirements(OrderSO order)
        {
            ClearContainer(_coffeeRequirementsContainer);
            if (order.coffeeRequirements == null)
            {
                return;
            }

            foreach (CoffeeRequirementData requirement in order.coffeeRequirements)
            {
                if (requirement.bean == null || _coffeeRequirementItemPrefab == null)
                {
                    continue;
                }

                GameObject item = Instantiate(_coffeeRequirementItemPrefab, _coffeeRequirementsContainer);
                item.GetComponent<CoffeeRequirementItemUI>()?.Init(requirement);
            }
        }

        /// <summary>
        /// 显示辅助液需求列表。
        /// </summary>
        /// <param name="order">订单配置。</param>
        private void DisplayLiquidRequirements(OrderSO order)
        {
            ClearContainer(_liquidRequirementsContainer);
            if (order.liquidRequirements == null)
            {
                return;
            }

            foreach (LiquidRequirementData requirement in order.liquidRequirements)
            {
                if (requirement.liquid == null || _liquidRequirementItemPrefab == null)
                {
                    continue;
                }

                GameObject item = Instantiate(_liquidRequirementItemPrefab, _liquidRequirementsContainer);
                item.GetComponent<LiquidRequirementItemUI>()?.Init(requirement);
            }
        }

        /// <summary>
        /// 显示小料需求列表。
        /// </summary>
        /// <param name="order">订单配置。</param>
        private void DisplayToppingRequirements(OrderSO order)
        {
            ClearContainer(_toppingRequirementsContainer);
            if (order.toppingRequirement == null || order.toppingRequirement.requiredToppings == null)
            {
                return;
            }

            foreach (ToppingSO topping in order.toppingRequirement.requiredToppings)
            {
                if (topping == null || _toppingRequirementItemPrefab == null)
                {
                    continue;
                }

                GameObject item = Instantiate(_toppingRequirementItemPrefab, _toppingRequirementsContainer);
                item.GetComponent<ToppingRequirementItemUI>()?.Init(topping);
            }
        }

        /// <summary>
        /// 清理指定需求容器下的所有子对象。
        /// </summary>
        /// <param name="container">需要清理的容器。</param>
        private void ClearContainer(Transform container)
        {
            if (container == null)
            {
                return;
            }

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 清空详情面板的需求内容。
        /// </summary>
        private void ClearDetail()
        {
            ClearContainer(_coffeeRequirementsContainer);
            ClearContainer(_liquidRequirementsContainer);
            ClearContainer(_toppingRequirementsContainer);
        }
    }
}
