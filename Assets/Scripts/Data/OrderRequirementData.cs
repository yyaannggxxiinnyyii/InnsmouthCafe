using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 咖啡液需求数据
    /// 定义单段咖啡液的需求（豆种+研磨度+容量）
    /// </summary>
    [System.Serializable]
    public class CoffeeRequirementData
    {
        [Header("咖啡豆")]
        [Tooltip("咖啡豆配置")]
        public BeanSO bean;

        [Header("研磨度")]
        [Tooltip("研磨程度")]
        public GrindType grindType;

        [Header("目标容量")]
        [Tooltip("目标咖啡液容量（ml），必须是25ml的倍数")]
        public int targetVolume;
    }

    /// <summary>
    /// 辅助液需求数据
    /// 定义单种辅助液的需求（液体+容量）
    /// </summary>
    [System.Serializable]
    public class LiquidRequirementData
    {
        [Header("辅助液")]
        [Tooltip("辅助液配置")]
        public LiquidSO liquid;

        [Header("目标容量")]
        [Tooltip("目标辅助液容量（ml）")]
        public int targetVolume;
    }

    /// <summary>
    /// 小料需求数据
    /// 定义小料需求（指定小料+属性需求）
    /// </summary>
    [System.Serializable]
    public class ToppingRequirementData
    {
        [Header("指定小料")]
        [Tooltip("要求的具体小料列表")]
        public List<ToppingSO> requiredToppings;

        [Header("属性需求")]
        [Tooltip("要求的小料属性标签列表")]
        public List<ToppingTagSO> requiredTags;
    }

    /// <summary>
    /// 订单需求数据（运行时）
    /// 由OrderSO在运行时转换而来
    /// 用于传递给CoffeeCraftManager进行制作
    /// </summary>
    [System.Serializable]
    public class OrderRequirementData
    {
        [Header("订单信息")]
        [Tooltip("订单名称")]
        public string orderName;

        [Tooltip("订单描述")]
        public string orderDescription;

        [Header("目标容量")]
        [Tooltip("目标总容量（咖啡液 + 辅助液总和，ml）")]
        public float targetTotalVolume;

        [Header("推荐容器")]
        [Tooltip("推荐杯子最小容量（ml）")]
        public float recommendedCupCapacity;

        [Header("咖啡液需求")]
        [Tooltip("所有咖啡液需求列表")]
        public List<CoffeeRequirementData> coffeeRequirements = new();

        [Header("辅助液需求")]
        [Tooltip("所有辅助液需求列表")]
        public List<LiquidRequirementData> liquidRequirements = new();

        [Header("小料需求")]
        [Tooltip("小料需求")]
        public ToppingRequirementData toppingRequirement = new();

        [Header("难度配置")]
        [Tooltip("订单难度等级（1-5）")]
        public int difficultyLevel = 1;
    }
}
