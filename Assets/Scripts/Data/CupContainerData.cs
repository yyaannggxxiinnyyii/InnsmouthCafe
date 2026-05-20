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
    }
}
