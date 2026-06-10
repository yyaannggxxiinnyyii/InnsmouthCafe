using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 收集物配置数据
    /// 定义收集物的名称、描述和图标
    /// </summary>
    [CreateAssetMenu(fileName = "Collectible_", menuName = "InnsmouthCafe/Config/Collectible", order = 10)]
    public class CollectibleSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("收集物唯一ID（用于持久化存储）")]
        public string collectibleId;

        [Tooltip("收集物显示名称")]
        public string collectibleName;

        [TextArea(3, 5)]
        [Tooltip("收集物描述文本")]
        public string description;

        [Header("显示")]
        [Tooltip("收集物图标")]
        public Sprite icon;
    }
}
