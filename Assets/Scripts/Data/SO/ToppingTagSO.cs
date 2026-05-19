using UnityEngine;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 小料标签配置数据
    /// 定义小料属性标签（甜、苦、咸、脆、爆珠、异香）
    /// </summary>
    [CreateAssetMenu(fileName = "ToppingTag_", menuName = "InnsmouthCafe/Config/Topping Tag", order = 5)]
    public class ToppingTagSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("标签唯一ID")]
        public string tagId;

        [Tooltip("标签显示名称")]
        public string tagName;

        [Header("视觉资源")]
        [Tooltip("标签图标")]
        public Sprite icon;

        [Header("描述")]
        [TextArea(3, 5)]
        [Tooltip("标签描述文本")]
        public string description;

#if UNITY_EDITOR
        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // Error: tagId为空
            if (string.IsNullOrEmpty(tagId))
            {
                Debug.LogError($"[ToppingTagSO] 标签ID不能为空", this);
            }

            // Error: tagName为空
            if (string.IsNullOrEmpty(tagName))
            {
                Debug.LogError($"[ToppingTagSO] 标签名称不能为空", this);
            }

            // Error: icon为空
            if (icon == null)
            {
                Debug.LogError($"[{tagName}] 标签图标不能为空", this);
            }

            // Warning: description为空
            if (string.IsNullOrEmpty(description))
            {
                Debug.LogWarning($"[{tagName}] 标签描述为空，建议填写描述文本", this);
            }
        }
#endif
    }
}
