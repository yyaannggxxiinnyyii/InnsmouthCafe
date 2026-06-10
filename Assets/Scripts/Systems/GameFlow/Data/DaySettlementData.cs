using System.Collections.Generic;
using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 单日结算数据
    /// 记录一天的经营情况
    /// </summary>
    [System.Serializable]
    public class DaySettlementData
    {
        [Header("基础信息")]
        [Tooltip("天数（第几天）")]
        public int dayNumber;

        [Header("理智值")]
        [Tooltip("当天开始时的理智值")]
        public float sanityStart;

        [Tooltip("当天结束时的理智值")]
        public float sanityEnd;

        [Tooltip("当天理智值变化量")]
        public float sanityChange;

        [Header("顾客统计")]
        [Tooltip("接待顾客总数")]
        public int customersServed;

        [Tooltip("好评订单数")]
        public int satisfiedCount;

        [Tooltip("一般订单数")]
        public int neutralCount;

        [Tooltip("差评订单数")]
        public int dissatisfiedCount;

        [Header("理智值变化明细")]
        [Tooltip("当天所有理智值变化记录")]
        public List<SanityChangeEntry> sanityChangeEntries = new List<SanityChangeEntry>();

        /// <summary>
        /// 清空数据
        /// </summary>
        public void Clear()
        {
            dayNumber = 0;
            sanityStart = 0f;
            sanityEnd = 0f;
            sanityChange = 0f;
            customersServed = 0;
            satisfiedCount = 0;
            neutralCount = 0;
            dissatisfiedCount = 0;
            sanityChangeEntries.Clear();
        }

        public override string ToString()
        {
            return $"[第{dayNumber}天结算] 顾客:{customersServed}人 | " +
                   $"好评:{satisfiedCount} 一般:{neutralCount} 差评:{dissatisfiedCount} | " +
                   $"理智值:{sanityStart:F1} → {sanityEnd:F1} ({sanityChange:+0.0;-0.0})";
        }
    }

    /// <summary>
    /// 理智值变化条目
    /// 用于日结算列表展示
    /// </summary>
    [System.Serializable]
    public class SanityChangeEntry
    {
        [Tooltip("变化原因")]
        public string reason;

        [Tooltip("变化量")]
        public float delta;

        public SanityChangeEntry(string reason, float delta)
        {
            this.reason = reason;
            this.delta = delta;
        }

        public override string ToString()
        {
            string sign = delta >= 0 ? "+" : "";
            return $"{reason} {sign}{delta:F1}";
        }
    }
}
