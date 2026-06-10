using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 订单配置数据
    /// 定义单个订单的完整需求（咖啡液、辅助液、小料、点单文本）
    /// 订单难度由所属OrderPoolSO决定，不在此存储
    /// </summary>
    [CreateAssetMenu(fileName = "Order_", menuName = "InnsmouthCafe/Config/Order", order = 6)]
    public class OrderSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("订单唯一ID")]
        public string orderId;

        [Tooltip("订单显示名称")]
        public string orderName;

        [Header("点单文本")]
        [TextArea(3, 5)]
        [Tooltip("顾客点单时的对话文本列表（随机一条）")]
        public List<string> orderDialogueTexts;

        [Header("咖啡液需求")]
        [Tooltip("咖啡液需求列表（至少1段）")]
        public List<CoffeeRequirementData> coffeeRequirements;

        [Header("辅助液需求")]
        [Tooltip("辅助液需求列表（可为空）")]
        public List<LiquidRequirementData> liquidRequirements;

        [Header("小料需求")]
        [Tooltip("小料需求（可为空）")]
        public ToppingRequirementData toppingRequirement;

        [Header("过滤规则")]
        [Tooltip("是否忽略材料解锁过滤。勾选后订单即使包含未解锁辅助液/小料，也允许被抽取。")]
        public bool ignoreIngredientUnlockFilter = false;

        /// <summary>
        /// 目标总量（自动计算）
        /// </summary>
        public int TargetTotalVolume
        {
            get
            {
                int total = 0;

                if (coffeeRequirements != null)
                {
                    foreach (var coffee in coffeeRequirements)
                    {
                        total += coffee.targetVolume;
                    }
                }

                if (liquidRequirements != null)
                {
                    foreach (var liquid in liquidRequirements)
                    {
                        total += liquid.targetVolume;
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// 转换为运行时数据
        /// 用于传递给 CoffeeCraftManager
        /// </summary>
        public OrderRequirementData ToData()
        {
            return new OrderRequirementData
            {
                orderName = this.orderName,
                orderDescription = "",
                targetTotalVolume = this.TargetTotalVolume,
                recommendedCupCapacity = this.TargetTotalVolume * 1.1f,
                coffeeRequirements = new List<CoffeeRequirementData>(this.coffeeRequirements ?? new List<CoffeeRequirementData>()),
                liquidRequirements = new List<LiquidRequirementData>(this.liquidRequirements ?? new List<LiquidRequirementData>()),
                toppingRequirement = this.toppingRequirement,
                difficultyLevel = 1
            };
        }

#if UNITY_EDITOR
        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // 校验逻辑将在Task 4.1中实现
        }
#endif
    }
}
