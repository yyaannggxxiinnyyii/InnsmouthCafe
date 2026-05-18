using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 咖啡豆数量数据
    /// 记录单种咖啡豆的类型和克数
    /// </summary>
    [System.Serializable]
    public class BeanAmountData
    {
        /// <summary>
        /// 咖啡豆类型
        /// </summary>
        public BeanType beanType;

        /// <summary>
        /// 咖啡豆克数
        /// </summary>
        public float amountGram;

        public BeanAmountData(BeanType type, float amount)
        {
            beanType = type;
            amountGram = amount;
        }
    }

    /// <summary>
    /// 辅助液数量数据
    /// 记录单种辅助液的类型和毫升数
    /// </summary>
    [System.Serializable]
    public class LiquidAmountData
    {
        /// <summary>
        /// 辅助液类型
        /// </summary>
        public LiquidType liquidType;

        /// <summary>
        /// 辅助液毫升数
        /// </summary>
        public float amountMl;

        public LiquidAmountData(LiquidType type, float amount)
        {
            liquidType = type;
            amountMl = amount;
        }
    }

    /// <summary>
    /// 小料实例数据
    /// 记录单个小料的类型和锚点索引
    /// </summary>
    [System.Serializable]
    public class ToppingInstanceData
    {
        /// <summary>
        /// 小料类型
        /// </summary>
        public ToppingType toppingType;

        /// <summary>
        /// 锚点索引（用于UI显示位置）
        /// </summary>
        public int anchorIndex;

        public ToppingInstanceData(ToppingType type, int index)
        {
            toppingType = type;
            anchorIndex = index;
        }
    }

    /// <summary>
    /// 当前咖啡数据
    /// 记录玩家当前制作的咖啡的完整信息
    /// 用于评分系统的主要输入数据
    /// </summary>
    [System.Serializable]
    public class CoffeeData
    {
        #region 基础信息

        /// <summary>
        /// 当前杯型
        /// </summary>
        public CupType cupType;

        #endregion

        #region 咖啡豆相关

        /// <summary>
        /// 咖啡豆组成列表（支持混豆）
        /// </summary>
        public List<BeanAmountData> beans = new List<BeanAmountData>();

        /// <summary>
        /// 研磨程度（可为null表示未研磨）
        /// </summary>
        public GrindType? grindType;

        /// <summary>
        /// 当前总豆重（克）
        /// </summary>
        public float TotalBeanWeight
        {
            get
            {
                float total = 0f;
                foreach (var bean in beans)
                {
                    total += bean.amountGram;
                }
                return total;
            }
        }

        #endregion

        #region 萃取相关

        /// <summary>
        /// 是否已完成萃取
        /// </summary>
        public bool hasExtracted;

        /// <summary>
        /// 咖啡液容量（毫升）
        /// 计算公式：总豆重 × 5ml
        /// </summary>
        public float coffeeLiquidAmount;

        #endregion

        #region 辅助液相关

        /// <summary>
        /// 辅助液列表（同类液体合并记录）
        /// </summary>
        public List<LiquidAmountData> liquids = new List<LiquidAmountData>();

        /// <summary>
        /// 辅助液总量（毫升）
        /// </summary>
        public float TotalLiquidAmount
        {
            get
            {
                float total = 0f;
                foreach (var liquid in liquids)
                {
                    total += liquid.amountMl;
                }
                return total;
            }
        }

        #endregion

        #region 小料相关

        /// <summary>
        /// 小料列表（最多20个）
        /// </summary>
        public List<ToppingInstanceData> toppings = new List<ToppingInstanceData>();

        #endregion

        #region 容量相关

        /// <summary>
        /// 当前总容量（毫升）
        /// 计算公式：咖啡液 + 辅助液总量
        /// </summary>
        public float CurrentTotalVolume
        {
            get
            {
                return coffeeLiquidAmount + TotalLiquidAmount;
            }
        }

        /// <summary>
        /// 是否超过标准容量
        /// </summary>
        public bool isOverStandardVolume;

        /// <summary>
        /// 是否溢出（超过最大容量）
        /// </summary>
        public bool isOverflowed;

        #endregion

        #region 重做相关

        /// <summary>
        /// 重做次数（用于统计）
        /// </summary>
        public int redoCount;

        #endregion

        #region 辅助方法

        /// <summary>
        /// 添加咖啡豆
        /// 如果已存在该类型豆子，则累加克数；否则新增
        /// </summary>
        public void AddBean(BeanType beanType, float amount)
        {
            var existingBean = beans.Find(b => b.beanType == beanType);
            if (existingBean != null)
            {
                existingBean.amountGram += amount;
            }
            else
            {
                beans.Add(new BeanAmountData(beanType, amount));
            }
        }

        /// <summary>
        /// 添加辅助液
        /// 同类液体自动合并
        /// </summary>
        public void AddLiquid(LiquidType liquidType, float amount)
        {
            var existingLiquid = liquids.Find(l => l.liquidType == liquidType);
            if (existingLiquid != null)
            {
                existingLiquid.amountMl += amount;
            }
            else
            {
                liquids.Add(new LiquidAmountData(liquidType, amount));
            }
        }

        /// <summary>
        /// 添加小料
        /// </summary>
        public void AddTopping(ToppingType toppingType)
        {
            int anchorIndex = toppings.Count;
            toppings.Add(new ToppingInstanceData(toppingType, anchorIndex));
        }

        /// <summary>
        /// 移除指定类型的最后一个小料
        /// </summary>
        public bool RemoveLastToppingOfType(ToppingType toppingType)
        {
            for (int i = toppings.Count - 1; i >= 0; i--)
            {
                if (toppings[i].toppingType == toppingType)
                {
                    toppings.RemoveAt(i);
                    RefreshToppingAnchors();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 刷新小料锚点索引（移除后自动补位）
        /// </summary>
        private void RefreshToppingAnchors()
        {
            for (int i = 0; i < toppings.Count; i++)
            {
                toppings[i].anchorIndex = i;
            }
        }

        /// <summary>
        /// 清空所有咖啡豆
        /// </summary>
        public void ClearBeans()
        {
            beans.Clear();
        }

        /// <summary>
        /// 清空整杯咖啡（重做）
        /// </summary>
        public void ClearAll()
        {
            cupType = CupType.Small;
            beans.Clear();
            grindType = null;
            hasExtracted = false;
            coffeeLiquidAmount = 0f;
            liquids.Clear();
            toppings.Clear();
            isOverStandardVolume = false;
            isOverflowed = false;
            redoCount++;
        }

        #endregion
    }
}
