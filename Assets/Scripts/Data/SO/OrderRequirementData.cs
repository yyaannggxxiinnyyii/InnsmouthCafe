using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 咖啡液需求数据
    /// 定义订单中对单种咖啡液的要求
    /// </summary>
    [System.Serializable]
    public class CoffeeRequirementData
    {
        [Tooltip("要求的豆种")]
        public BeanType beanType;

        [Tooltip("要求的研磨程度")]
        public GrindType grindType;

        [Tooltip("目标咖啡液量（ml）")]
        public float targetVolume;
    }

    /// <summary>
    /// 辅助液需求数据
    /// 定义订单中对单种辅助液的要求
    /// </summary>
    [System.Serializable]
    public class LiquidRequirementData
    {
        [Tooltip("要求的辅助液类型")]
        public LiquidType liquidType;

        [Tooltip("目标辅助液量（ml）")]
        public float targetVolume;
    }

    /// <summary>
    /// 小料需求数据
    /// 定义订单中对小料的要求
    /// </summary>
    [System.Serializable]
    public class ToppingRequirementData
    {
        [Tooltip("指定的小料类型列表")]
        public List<ToppingType> specificToppings = new();

        [Tooltip("要求的小料属性标签列表")]
        public List<ToppingTag> requiredTags = new();
    }

    /// <summary>
    /// 订单需求数据
    /// 定义顾客期望的完整咖啡标准
    /// </summary>
    [System.Serializable]
    public class OrderRequirementData
    {
        [Header("目标总量")]
        [Tooltip("订单要求的目标总容量（ml）")]
        public float targetTotalVolume;

        [Header("咖啡液需求")]
        [Tooltip("所有咖啡液需求列表")]
        public List<CoffeeRequirementData> coffeeRequirements = new();

        [Header("辅助液需求")]
        [Tooltip("所有辅助液需求列表")]
        public List<LiquidRequirementData> liquidRequirements = new();

        [Header("小料需求")]
        [Tooltip("小料需求")]
        public ToppingRequirementData toppingRequirement = new();

        [Header("推荐杯子")]
        [Tooltip("推荐使用的杯子容量（ml）")]
        public float recommendedCupCapacity;
    }
}
