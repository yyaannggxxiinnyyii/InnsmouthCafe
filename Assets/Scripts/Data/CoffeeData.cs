using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 小料实例数据
    /// 记录单个小料的类型和添加顺序
    /// </summary>
    [System.Serializable]
    public class ToppingInstanceData
    {
        [Tooltip("小料类型")]
        public ToppingType toppingType;

        [Tooltip("添加顺序索引（用于锚点定位）")]
        public int orderIndex;
    }

    /// <summary>
    /// 咖啡成品数据（V0.2）
    /// 记录一杯咖啡的完整制作信息
    /// </summary>
    [System.Serializable]
    public class CoffeeData
    {
        [Header("杯子信息")]
        [Tooltip("实际选择的杯子")]
        public CupContainerData selectedCup;

        [Header("咖啡液段")]
        [Tooltip("所有咖啡液段列表（按萃取顺序）")]
        public List<CoffeeExtractSegmentData> coffeeSegments = new List<CoffeeExtractSegmentData>();

        [Header("辅助液段")]
        [Tooltip("所有辅助液段列表（按添加顺序）")]
        public List<LiquidSegmentData> liquidSegments = new List<LiquidSegmentData>();

        [Header("小料列表")]
        [Tooltip("所有小料实例列表（按添加顺序）")]
        public List<ToppingInstanceData> toppings = new List<ToppingInstanceData>();

        [Header("容量信息")]
        [Tooltip("当前总容量（ml）")]
        public float currentTotalVolume;

        [Tooltip("是否已溢出")]
        public bool isOverflowed;

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            selectedCup = null;
            coffeeSegments.Clear();
            liquidSegments.Clear();
            toppings.Clear();
            currentTotalVolume = 0f;
            isOverflowed = false;
        }

        /// <summary>
        /// 计算咖啡液总量
        /// </summary>
        public float GetTotalCoffeeVolume()
        {
            float total = 0f;
            foreach (var segment in coffeeSegments)
            {
                total += segment.extractedVolume;
            }
            return total;
        }

        /// <summary>
        /// 计算辅助液总量
        /// </summary>
        public float GetTotalLiquidVolume()
        {
            float total = 0f;
            foreach (var segment in liquidSegments)
            {
                total += segment.amountMl;
            }
            return total;
        }
    }
}
