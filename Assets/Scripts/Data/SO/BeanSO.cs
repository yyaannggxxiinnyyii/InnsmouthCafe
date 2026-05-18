using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 咖啡豆配置数据
    /// 定义每种咖啡豆的属性和解锁状态
    /// </summary>
    [CreateAssetMenu(fileName = "Bean_", menuName = "InnsmouthCafe/Config/Bean Config", order = 2)]
    public class BeanSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("咖啡豆类型")]
        public BeanType beanType;

        [Tooltip("咖啡豆名称")]
        public string beanName;

        [Tooltip("咖啡豆描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("UI显示")]
        [Tooltip("咖啡豆图标")]
        public Sprite beanIcon;

        [Tooltip("咖啡豆桶图标（制作界面1使用）")]
        public Sprite beanBarrelIcon;

        [Tooltip("咖啡豆颜色（用于豆量进度条显示）")]
        public Color beanColor = new Color(0.6f, 0.4f, 0.2f); // 默认棕色

        [Header("解锁设置")]
        [Tooltip("是否永久解锁（false表示一次性消耗型）")]
        public bool isPermanent = true;

        [Tooltip("初始使用次数（仅对一次性豆有效，-1表示无限）")]
        public int initialUseCount = -1;

        [Header("特殊属性")]
        [Tooltip("是否为特殊咖啡豆（影响UI显示和特效）")]
        public bool isSpecial = false;

        [Tooltip("特殊豆种标签（如：深海豆、珊瑚豆等）")]
        public string specialTag = "";

        /// <summary>
        /// 获取显示名称（特殊豆会加上标签）
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (isSpecial && !string.IsNullOrEmpty(specialTag))
                {
                    return $"{beanName}【{specialTag}】";
                }
                return beanName;
            }
        }

        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // 如果是永久解锁，使用次数应该为-1
            if (isPermanent && initialUseCount != -1)
            {
                Debug.LogWarning($"[{beanName}] 永久解锁的咖啡豆，使用次数应设置为 -1（无限）");
            }

            // 如果是一次性豆，使用次数应该大于0
            if (!isPermanent && initialUseCount <= 0)
            {
                Debug.LogWarning($"[{beanName}] 一次性咖啡豆的使用次数应大于 0");
            }

            // 特殊豆应该有特殊标签
            if (isSpecial && string.IsNullOrEmpty(specialTag))
            {
                Debug.LogWarning($"[{beanName}] 特殊咖啡豆应该设置特殊标签");
            }
        }
    }
}
