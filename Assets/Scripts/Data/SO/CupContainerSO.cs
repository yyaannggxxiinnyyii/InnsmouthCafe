using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 杯子容器配置ScriptableObject
    /// 用于在Unity中创建杯子配置资源
    /// </summary>
    [CreateAssetMenu(fileName = "Cup_", menuName = "InnsmouthCafe/Config/Cup Container", order = 1)]
    public class CupContainerSO : ScriptableObject
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

        /// <summary>
        /// 转换为CupContainerData
        /// </summary>
        public CupContainerData ToData()
        {
            return new CupContainerData
            {
                cupId = this.cupId,
                cupName = this.cupName,
                capacity = this.capacity,
                cupSprite = this.cupSprite
            };
        }
    }
}
