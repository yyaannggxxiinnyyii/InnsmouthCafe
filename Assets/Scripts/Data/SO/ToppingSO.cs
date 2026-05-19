using UnityEngine;
using System.Collections.Generic;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 小料配置数据
    /// 定义小料的基础属性、标签和显示信息
    /// </summary>
    [CreateAssetMenu(fileName = "Topping_", menuName = "InnsmouthCafe/Config/Topping Config", order = 4)]
    public class ToppingSO : ScriptableObject
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

        [Header("小料属性")]
        [Tooltip("小料拥有的标签列表")]
        public List<ToppingTagSO> tags;

        [Header("描述")]
        [TextArea(3, 5)]
        [Tooltip("小料描述文本")]
        public string description;

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

            // Error: tags中存在null
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    if (tags[i] == null)
                    {
                        Debug.LogError($"[{toppingName}] 小料标签列表中存在空引用（索引{i}）", this);
                    }
                }

                // Error: tags中重复
                for (int i = 0; i < tags.Count; i++)
                {
                    for (int j = i + 1; j < tags.Count; j++)
                    {
                        if (tags[i] != null && tags[j] != null && tags[i] == tags[j])
                        {
                            Debug.LogError($"[{toppingName}] 小料标签列表中存在重复标签：{tags[i].tagName}", this);
                        }
                    }
                }
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
