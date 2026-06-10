using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 咖啡豆配置数据
    /// 定义咖啡豆的基础属性和显示信息
    /// </summary>
    [CreateAssetMenu(fileName = "Bean_", menuName = "InnsmouthCafe/Config/Bean Config", order = 2)]
    public class BeanSO : ScriptableObject, IItemTooltipSource
    {
        [Header("基础信息")]
        [Tooltip("咖啡豆唯一ID")]
        public string beanId;

        [Tooltip("咖啡豆显示名称")]
        public string beanName;

        [Header("视觉资源")]
        [Tooltip("咖啡豆图标")]
        public Sprite icon;

        [Tooltip("咖啡豆桶图标（制作界面1使用）")]
        public Sprite beanBarrelIcon;

        [Tooltip("咖啡豆显示颜色（用于取豆条、咖啡液段）")]
        public Color displayColor = new Color(0.6f, 0.4f, 0.2f); // 默认棕色

        [Header("描述")]
        [TextArea(3, 5)]
        [Tooltip("咖啡豆描述文本（用于图鉴、悬停提示）")]
        public string description;

        public string TooltipTitle => beanName;
        public string TooltipDescription => description;

#if UNITY_EDITOR
        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // Error: beanId为空
            if (string.IsNullOrEmpty(beanId))
            {
                Debug.LogError($"[BeanSO] 咖啡豆ID不能为空", this);
            }

            // Error: beanName为空
            if (string.IsNullOrEmpty(beanName))
            {
                Debug.LogError($"[BeanSO] 咖啡豆名称不能为空", this);
            }

            // Error: icon为空
            if (icon == null)
            {
                Debug.LogError($"[{beanName}] 咖啡豆图标不能为空", this);
            }

            // Warning: description为空
            if (string.IsNullOrEmpty(description))
            {
                Debug.LogWarning($"[{beanName}] 咖啡豆描述为空，建议填写描述文本", this);
            }
        }
#endif
    }
}
