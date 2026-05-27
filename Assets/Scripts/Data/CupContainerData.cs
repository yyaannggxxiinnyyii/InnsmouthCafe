using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 杯子容器数据
    /// 杯子只决定容器容量和适配评分，不直接决定订单目标容量
    /// </summary>
    [System.Serializable]
    public class CupContainerData
    {
        [Header("基础信息")]
        [Tooltip("杯子唯一ID")]
        public string cupId;

        [Tooltip("杯子显示名称")]
        public string cupName;

        [Header("容量配置")]
        [Tooltip("杯子最大容量（ml）")]
        public float capacity;

        [Header("视觉资源")]
        [Tooltip("杯子图标")]
        public Sprite cupSprite;

        [Tooltip("0%~25% 填充贴图")]
        public Sprite fillSprite_0_25;

        [Tooltip("25%~50% 填充贴图")]
        public Sprite fillSprite_25_50;

        [Tooltip("50%~75% 填充贴图")]
        public Sprite fillSprite_50_75;

        [Tooltip("75%~100% 填充贴图")]
        public Sprite fillSprite_75_100;

        /// <summary>
        /// 根据填充比例（0~1）返回对应阶段贴图，未配置时回退到 cupSprite
        /// 0        → cupSprite（空杯）
        /// 0~25%    → fillSprite_0_25
        /// 25%~50%  → fillSprite_25_50
        /// 50%~75%  → fillSprite_50_75
        /// 75%~100% → fillSprite_75_100
        /// </summary>
        public Sprite GetSpriteForFillRatio(float ratio)
        {
            Sprite result;

            if (ratio <= 0f)
                result = cupSprite;
            else if (ratio >= 0.75f)
                result = fillSprite_75_100;
            else if (ratio >= 0.5f)
                result = fillSprite_50_75;
            else if (ratio >= 0.25f)
                result = fillSprite_25_50;
            else
                result = fillSprite_0_25;

            return result != null ? result : cupSprite;
        }
    }
}
