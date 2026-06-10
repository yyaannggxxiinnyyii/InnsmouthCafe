using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 咖啡萃取段数据
    /// 记录单次萃取产生的咖啡液信息
    /// 一杯咖啡可包含多个萃取段
    /// </summary>
    [System.Serializable]
    public class CoffeeExtractSegmentData
    {
        [Header("豆种信息")]
        [Tooltip("咖啡豆配置")]
        public BeanSO bean;

        [Header("研磨信息")]
        [Tooltip("研磨程度")]
        public GrindType grindType;

        [Header("豆量信息")]
        [Tooltip("使用的咖啡豆克数")]
        public float beanGram;

        [Header("萃取结果")]
        [Tooltip("萃取出的咖啡液毫升数（1g豆=5ml液）")]
        public float extractedVolume;
    }
}
