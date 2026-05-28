using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 订单小票UI组件
    /// 显示咖啡液需求、辅助液需求、小料需求
    /// 支持点击把手收起/展开（向上滑出，把手常驻）
    /// </summary>
    public class OrderTicketUI : MonoBehaviour
    {
        [Header("需求容器")]
        [SerializeField] [Tooltip("咖啡液需求容器")]
        private Transform _coffeeRequirementsContainer;

        [SerializeField] [Tooltip("辅助液需求容器")]
        private Transform _liquidRequirementsContainer;

        [SerializeField] [Tooltip("小料需求容器")]
        private Transform _toppingRequirementsContainer;

        [Header("预制体")]
        [SerializeField] [Tooltip("咖啡液需求项预制体（需挂载CoffeeRequirementItemUI）")]
        private GameObject _coffeeRequirementItemPrefab;

        [SerializeField] [Tooltip("辅助液需求项预制体（需挂载LiquidRequirementItemUI）")]
        private GameObject _liquidRequirementItemPrefab;

        [SerializeField] [Tooltip("小料需求项预制体（需挂载ToppingRequirementItemUI）")]
        private GameObject _toppingRequirementItemPrefab;

        [Header("收起/展开")]
        [SerializeField] [Tooltip("把手按钮（位于小票底部，点击触发收起/展开）")]
        private Button _toggleButton;

        [SerializeField] [Tooltip("收起/展开动画时长")]
        private float _toggleDuration = 0.3f;

        [SerializeField] [Tooltip("动画曲线")]
        private Ease _toggleEase = Ease.OutQuart;

        private RectTransform _ticketRect;
        private Vector2 _defaultPosition;
        private Vector2 _expandedPosition;
        private bool _hasRecordedDefaultPosition = false;
        private bool _isCollapsed = false;
        private bool _isAnimating = false;

        private void Awake()
        {
            _ticketRect = GetComponent<RectTransform>();
            _toggleButton?.onClick.AddListener(OnToggleClicked);
        }

        private void Start()
        {
            // 记录场景中的原始默认位置，作为小票基准位置
            _defaultPosition = _ticketRect.anchoredPosition;
            _expandedPosition = _defaultPosition;
            _hasRecordedDefaultPosition = true;
        }

        // ── 收起/展开 ─────────────────────────────────────────

        private void OnToggleClicked()
        {
            if (_isAnimating) return;

            if (_isCollapsed)
                Expand();
            else
                Collapse();
        }

        /// <summary>收起小票（向上滑出，把手留在原位）</summary>
        public void Collapse()
        {
            if (_isCollapsed || _isAnimating) return;

            _isAnimating = true;
            _isCollapsed = true;

            float offset = _ticketRect.rect.height * 2f;

            _ticketRect.DOKill();
            _ticketRect.DOAnchorPosY(_expandedPosition.y + offset, _toggleDuration)
                .SetEase(_toggleEase)
                .OnComplete(() => _isAnimating = false);
        }

        /// <summary>展开小票（滑回原位）</summary>
        public void Expand()
        {
            if (!_isCollapsed || _isAnimating) return;

            _isAnimating = true;
            _isCollapsed = false;

            _ticketRect.DOKill();
            _ticketRect.DOAnchorPosY(_expandedPosition.y, _toggleDuration)
                .SetEase(_toggleEase)
                .OnComplete(() => _isAnimating = false);
        }

        // ── 刷新小票 ──────────────────────────────────────────

        /// <summary>
        /// 刷新订单小票显示
        /// </summary>
        public void RefreshTicket(OrderSO order)
        {
            if (order == null)
            {
                ClearTicket();
                return;
            }

            Debug.Log($"[OrderTicket] 刷新订单小票: {order.orderName}");

            DisplayCoffeeRequirements(order);
            DisplayLiquidRequirements(order);
            DisplayToppingRequirements(order);

            StartCoroutine(RefreshLayoutNextFrame());
        }

        /// <summary>
        /// 在下一帧刷新布局，并更新展开位置记录
        /// </summary>
        private IEnumerator RefreshLayoutNextFrame()
        {
            yield return null;

            ForceRebuildLayout();

            // 布局重建后更新展开位置（内容高度可能变化）
            if (_hasRecordedDefaultPosition)
            {
                _expandedPosition = _ticketRect.anchoredPosition;
            }
        }

        /// <summary>
        /// 强制重建布局
        /// </summary>
        private void ForceRebuildLayout()
        {
            if (_coffeeRequirementsContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_coffeeRequirementsContainer as RectTransform);
            }

            if (_liquidRequirementsContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_liquidRequirementsContainer as RectTransform);
            }

            if (_toppingRequirementsContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_toppingRequirementsContainer as RectTransform);
            }

            // 如果小票面板本身也有LayoutGroup，也刷新它
            var parentRect = transform as RectTransform;
            if (parentRect != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }

        /// <summary>
        /// 显示咖啡液需求
        /// </summary>
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

        /// <summary>
        /// 显示辅助液需求
        /// </summary>
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

        /// <summary>
        /// 显示小料需求
        /// </summary>
        private void DisplayToppingRequirements(OrderSO order)
        {
            ClearContainer(_toppingRequirementsContainer);

            if (order.toppingRequirement == null) return;

            // 指定小料
            if (order.toppingRequirement.requiredToppings != null)
            {
                foreach (var topping in order.toppingRequirement.requiredToppings)
                {
                    if (topping == null) continue;

                    var item = Instantiate(_toppingRequirementItemPrefab, _toppingRequirementsContainer);
                    item.GetComponent<ToppingRequirementItemUI>().Init(topping);
                }
            }

            // 属性需求
            if (order.toppingRequirement.requiredTags != null)
            {
                foreach (var tag in order.toppingRequirement.requiredTags)
                {
                    if (tag == null) continue;

                    var item = Instantiate(_toppingRequirementItemPrefab, _toppingRequirementsContainer);
                    item.GetComponent<ToppingRequirementItemUI>().Init(tag);
                }
            }
        }

        /// <summary>
        /// 清空容器中的所有子对象
        /// </summary>
        private void ClearContainer(Transform container)
        {
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 清空整个小票
        /// </summary>
        public void ClearTicket()
        {
            ClearContainer(_coffeeRequirementsContainer);
            ClearContainer(_liquidRequirementsContainer);
            ClearContainer(_toppingRequirementsContainer);
        }

        /// <summary>
        /// 隐藏前重置小票状态，确保下一单重新记录展开基准
        /// </summary>
        public void ResetTicketState()
        {
            StopAllCoroutines();
            _ticketRect.DOKill();
            _isAnimating = false;
            _isCollapsed = false;

            ClearTicket();

            if (_ticketRect != null && _hasRecordedDefaultPosition)
            {
                _ticketRect.anchoredPosition = _defaultPosition;
                _expandedPosition = _defaultPosition;
            }
        }

        /// <summary>
        /// 重置并重新记录当前展开位置
        /// </summary>
        public void RebuildExpandedPosition()
        {
            if (_ticketRect == null)
            {
                return;
            }

            _expandedPosition = _ticketRect.anchoredPosition;
        }
    }
}
