using UnityEngine;
using System.Collections.Generic;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 订单小票UI组件
    /// 显示咖啡液需求、辅助液需求、小料需求
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

            // 使用协程延迟刷新布局
            StartCoroutine(RefreshLayoutNextFrame());
        }

        /// <summary>
        /// 在下一帧刷新布局
        /// </summary>
        private System.Collections.IEnumerator RefreshLayoutNextFrame()
        {
            // 等待一帧，让Unity完成子对象的实例化
            yield return null;

            // 强制刷新布局
            ForceRebuildLayout();
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
    }
}
