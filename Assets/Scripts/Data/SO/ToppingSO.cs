using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 小料配置数据
    /// 定义小料的基础属性和显示信息
    /// </summary>
    [CreateAssetMenu(fileName = "Topping_", menuName = "InnsmouthCafe/Config/Topping Config", order = 4)]
    public class ToppingSO : ScriptableObject, IItemTooltipSource
    {
        [Header("基础信息")]
        [Tooltip("小料唯一ID")]
        public string toppingId;

        [Tooltip("小料显示名称")]
        public string toppingName;

        [Header("视觉资源")]
        [Tooltip("小料图标（按钮显示）")]
        public Sprite icon;

        [Tooltip("小料实例图标（锚点显示）")]
        public Sprite instanceIcon;

        [Header("描述")]
        [TextArea(3, 5)]
        [Tooltip("小料描述文本")]
        public string description;

        public string TooltipTitle => toppingName;
        public string TooltipDescription => description;

#if UNITY_EDITOR
        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // Error: toppingId为空
            if (string.IsNullOrEmpty(toppingId))
            {
                Debug.LogError($"[ToppingSO] 小料ID不能为空", this);
            }

            // Error: toppingName为空
            if (string.IsNullOrEmpty(toppingName))
            {
                Debug.LogError($"[ToppingSO] 小料名称不能为空", this);
            }

            // Error: icon为空
            if (icon == null)
            {
                Debug.LogError($"[{toppingName}] 小料图标不能为空", this);
            }

            // Warning: description为空
            if (string.IsNullOrEmpty(description))
            {
                Debug.LogWarning($"[{toppingName}] 小料描述为空，建议填写描述文本", this);
            }
        }
#endif
    }
}
