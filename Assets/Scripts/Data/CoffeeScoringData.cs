using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 咖啡评分数据
    /// 记录咖啡的各个维度评分与最终结果
    /// </summary>
    [System.Serializable]
    public class CoffeeScoringData
    {
        [Header("各维度得分 (0-100)")]
        [Range(0, 100)]
        [Tooltip("咖啡液匹配得分")]
        public float coffeeMatchScore;

        [Range(0, 100)]
        [Tooltip("辅助液匹配得分")]
        public float liquidMatchScore;

        [Range(0, 100)]
        [Tooltip("小料匹配得分")]
        public float toppingMatchScore;

        [Range(0, 100)]
        [Tooltip("总量匹配得分")]
        public float volumeMatchScore;

        [Range(0, 100)]
        [Tooltip("杯子适配得分")]
        public float cupAdaptationScore;

        [Header("综合评分")]
        [Range(0, 100)]
        [Tooltip("加权后的最终得分 (0-100)")]
        public float finalScore;

        [Tooltip("最终品质等级")]
        public CoffeeQuality qualityLevel;

        [Header("反馈档位")]
        [Range(0, 2)]
        [Tooltip("顾客评价档位 (0=不满意, 1=一般, 2=满意)")]
        public int feedbackLevel;

        [Header("特殊情况标记")]
        [Tooltip("是否发生溢出")]
        public bool hasOverflow;

        [Tooltip("溢出导致的理智值损失")]
        public float overflowSanityLoss;

        [Tooltip("是否缺少必要成分")]
        public bool hasMissingComponents;

        [Header("调试信息")]
        [TextArea(3, 5)]
        [Tooltip("评分详细说明")]
        public string scoringDetail;

        public void Clear()
        {
            coffeeMatchScore = 0;
            liquidMatchScore = 0;
            toppingMatchScore = 0;
            volumeMatchScore = 0;
            cupAdaptationScore = 0;
            finalScore = 0;
            qualityLevel = CoffeeQuality.Terrible;
            feedbackLevel = 0;
            hasOverflow = false;
            overflowSanityLoss = 0;
            hasMissingComponents = false;
            scoringDetail = "";
        }

        public override string ToString()
        {
            return $"[咖啡评分] 最终得分: {finalScore:F1}/100 | 品质: {qualityLevel} | 反馈档位: {feedbackLevel}\n" +
                   $"咖啡液: {coffeeMatchScore:F1} | 辅助液: {liquidMatchScore:F1} | 小料: {toppingMatchScore:F1} | 总量: {volumeMatchScore:F1} | 杯子: {cupAdaptationScore:F1}\n" +
                   $"{scoringDetail}";
        }
    }
}
