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
    /// 预览状态：固定在左上角，小尺寸，点击展开
    /// 详细状态：居中放大，可上下拖拽，点击（非拖拽）收起
    /// 生成订单时自动展开详细状态
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

        [Header("预览/详细状态配置")]
        [SerializeField] [Tooltip("预览状态的缩放比例")]
        private float _previewScale = 0.35f;

        [SerializeField] [Tooltip("详细状态的缩放比例")]
        private float _detailScale = 1f;

        [SerializeField] [Tooltip("预览状态的锚点位置（左上角）")]
        private Vector2 _previewAnchoredPos = new Vector2(160f, -160f);

        [SerializeField] [Tooltip("详细状态的锚点位置（屏幕中央）")]
        private Vector2 _detailAnchoredPos = Vector2.zero;

        [Header("动画配置")]
        [SerializeField] [Tooltip("展开/收起动画时长")]
        private float _expandDuration = 0.35f;

        [SerializeField] [Tooltip("展开动画曲线")]
        private Ease _expandEase = Ease.OutBack;

        [SerializeField] [Tooltip("收起动画曲线")]
        private Ease _collapseEase = Ease.InBack;

        [Header("拖拽配置")]
        [SerializeField] [Tooltip("拖拽判定阈值（像素），超过此距离才算拖拽，否则算点击")]
        private float _dragThreshold = 10f;

        [SerializeField] [Tooltip("拖拽上边界（距屏幕顶部的最小距离，正值）")]
        private float _dragBoundTop = 50f;

        [SerializeField] [Tooltip("拖拽下边界（距屏幕底部的最小距离，正值）")]
        private float _dragBoundBottom = 50f;

        [Header("点击区域")]
        [SerializeField] [Tooltip("小票点击/拖拽区域按钮")]
        private Button _ticketClickButton;

        private RectTransform _ticketRect;
        private RectTransform _canvasRect;
        private bool _isExpanded = false;
        private bool _isAnimating = false;

        // 拖拽状态
        private bool _isDragging = false;
        private Vector2 _dragStartPointerPos;
        private Vector2 _dragStartAnchoredPos;

        private void Awake()
        {
            _ticketRect = GetComponent<RectTransform>();
            _ticketClickButton?.onClick.AddListener(OnTicketClicked);

            // 获取根Canvas的RectTransform用于边界计算
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                _canvasRect = canvas.GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplyPreviewStateImmediate();
        }

        // ── 点击回调 ──────────────────────────────────────────

        private void OnTicketClicked()
        {
            // 拖拽结束后 onClick 也会触发，用 _isDragging 标记过滤掉
            if (_isAnimating || _isDragging) return;

            if (_isExpanded)
                CollapseToPreview();
            else
                ExpandToDetail();
        }

        // ── 拖拽接口 ──────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isExpanded || _isAnimating) return;

            _dragStartPointerPos  = eventData.position;
            _dragStartAnchoredPos = _ticketRect.anchoredPosition;
            _isDragging = false; // 先不标记，等超过阈值再标记
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isExpanded || _isAnimating) return;

            Vector2 delta = eventData.position - _dragStartPointerPos;

            // 超过阈值才正式进入拖拽模式
            if (!_isDragging && Mathf.Abs(delta.y) > _dragThreshold)
                _isDragging = true;

            if (!_isDragging) return;

            // 停止当前 DOTween 动画，避免冲突
            _ticketRect.DOKill();

            // 只允许垂直拖拽
            float newY = _dragStartAnchoredPos.y + delta.y;

            // 计算边界（anchoredPosition 基于 Canvas 中心）
            float halfCanvasH = _canvasRect != null ? _canvasRect.rect.height * 0.5f : Screen.height * 0.5f;
            float halfTicketH = _ticketRect.rect.height * _detailScale * 0.5f;

            float minY = -halfCanvasH + halfTicketH + _dragBoundBottom;
            float maxY =  halfCanvasH - halfTicketH - _dragBoundTop;

            newY = Mathf.Clamp(newY, minY, maxY);
            _ticketRect.anchoredPosition = new Vector2(_ticketRect.anchoredPosition.x, newY);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // OnEndDrag 之后 onClick 会紧接着触发，延一帧再清标记
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
        /// 展开为详细小票（从左上角放大居中）
        /// </summary>
        public void ExpandToDetail()
        {
            if (_isExpanded || _isAnimating) return;

            _isAnimating = true;
            _isExpanded  = true;

            _ticketRect.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(_ticketRect.DOAnchorPos(_detailAnchoredPos, _expandDuration).SetEase(_expandEase));
            seq.Join(_ticketRect.DOScale(_detailScale, _expandDuration).SetEase(_expandEase));
            seq.OnComplete(() => _isAnimating = false);
        }

        /// <summary>
        /// 收起为预览小票（从当前位置缩回左上角）
        /// </summary>
        public void CollapseToPreview()
        {
            if (!_isExpanded || _isAnimating) return;

            _isAnimating = true;
            _isExpanded  = false;

            _ticketRect.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(_ticketRect.DOAnchorPos(_previewAnchoredPos, _expandDuration).SetEase(_collapseEase));
            seq.Join(_ticketRect.DOScale(_previewScale, _expandDuration).SetEase(_collapseEase));
            seq.OnComplete(() => _isAnimating = false);
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

            DisplayCoffeeRequirements(order);
            DisplayLiquidRequirements(order);
            DisplayToppingRequirements(order);

            StartCoroutine(RefreshLayoutThenExpand());
        }

        private IEnumerator RefreshLayoutThenExpand()
        {
            yield return null;
            ForceRebuildLayout();
            ExpandToDetail();
        }

        private void ForceRebuildLayout()
        {
            if (_coffeeRequirementsContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_coffeeRequirementsContainer as RectTransform);
            if (_liquidRequirementsContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_liquidRequirementsContainer as RectTransform);
            if (_toppingRequirementsContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_toppingRequirementsContainer as RectTransform);

            var parentRect = transform as RectTransform;
            if (parentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
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
        /// 重置小票状态（新一单时调用）
        /// </summary>
        public void ResetTicketState()
        {
            StopAllCoroutines();
            _ticketRect.DOKill();
            _isAnimating = false;
            _isExpanded  = false;
            _isDragging  = false;

            ClearTicket();
            ApplyPreviewStateImmediate();
        }

        private void ApplyPreviewStateImmediate()
        {
            _ticketRect.anchoredPosition = _previewAnchoredPos;
            _ticketRect.localScale       = Vector3.one * _previewScale;
        }
    }
}
