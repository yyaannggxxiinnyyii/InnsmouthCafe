using UnityEngine;
using System.Collections.Generic;

namespace InnsmouthCafe.Data
{
    /// <summary>
    /// 小料配置数据
    /// 定义每种小料的属性、标签和显示效果
    /// </summary>
    [CreateAssetMenu(fileName = "Topping_", menuName = "InnsmouthCafe/Config/Topping Config", order = 4)]
    public class ToppingSO : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("小料类型")]
        public ToppingType toppingType;

        [Tooltip("小料名称")]
        public string toppingName;

        [Tooltip("小料描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("UI显示")]
        [Tooltip("小料图标（按钮显示）")]
        public Sprite toppingIcon;

        [Tooltip("小料实例图标（锚点显示）")]
        public Sprite instanceIcon;

        [Header("小料属性")]
        [Tooltip("小料属性标签列表（可多选）")]
        public List<ToppingAttribute> attributes = new List<ToppingAttribute>();

        [Header("解锁设置")]
        [Tooltip("是否永久解锁（false表示一次性小料）")]
        public bool isPermanent = true;

        [Tooltip("初始使用次数（-1表示无限次，仅对一次性小料有效）")]
        public int initialUseCount = -1;

        [Header("特殊属性")]
        [Tooltip("是否为特殊小料")]
        public bool isSpecial = false;

        [Tooltip("特殊标签（用于显示特殊小料的额外信息）")]
        public string specialTag = "";

        /// <summary>
        /// 获取显示名称（特殊小料会附加标签）
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (isSpecial && !string.IsNullOrEmpty(specialTag))
                    return $"{toppingName}({specialTag})";
                return toppingName;
            }
        }

        /// <summary>
        /// 检查是否包含指定属性
        /// </summary>
        public bool HasAttribute(ToppingAttribute attribute)
        {
            return attributes.Contains(attribute);
        }

        /// <summary>
        /// 获取属性标签的显示文本
        /// </summary>
        public string GetAttributesText()
        {
            if (attributes == null || attributes.Count == 0)
                return "无属性";

            List<string> attrNames = new List<string>();
            foreach (var attr in attributes)
            {
                attrNames.Add(GetAttributeName(attr));
            }
            return string.Join("、", attrNames);
        }

        /// <summary>
        /// 获取属性的中文名称
        /// </summary>
        private string GetAttributeName(ToppingAttribute attribute)
        {
            switch (attribute)
            {
                case ToppingAttribute.Sweet: return "甜";
                case ToppingAttribute.Bitter: return "苦";
                case ToppingAttribute.Salty: return "咸";
                case ToppingAttribute.Crispy: return "脆";
                case ToppingAttribute.Boba: return "爆珠";
                case ToppingAttribute.ExoticAroma: return "异香";
                default: return attribute.ToString();
            }
        }

        /// <summary>
        /// 验证配置数据的合理性
        /// </summary>
        private void OnValidate()
        {
            // 检查属性标签是否为空
            if (attributes == null || attributes.Count == 0)
            {
                Debug.LogWarning($"[{toppingName}] 小料属性标签为空，建议至少添加一个属性标签");
            }

            // 检查一次性小料的使用次数
            if (!isPermanent && initialUseCount == -1)
            {
                Debug.LogWarning($"[{toppingName}] 一次性小料的使用次数为-1（无限次），建议设置具体次数或改为永久解锁");
            }

            // 检查特殊小料的标签
            if (isSpecial && string.IsNullOrEmpty(specialTag))
            {
                Debug.LogWarning($"[{toppingName}] 特殊小料未设置特殊标签，建议添加标签用于区分");
            }

            // 根据小料类型建议属性标签
            switch (toppingType)
            {
                case ToppingType.CaramelCrumbs:
                    if (!HasAttribute(ToppingAttribute.Sweet) && !HasAttribute(ToppingAttribute.Crispy))
                    {
                        Debug.Log($"[{toppingName}] 焦糖碎建议添加’甜’或’脆’属性");
                    }
                    break;

                case ToppingType.ChocolatePowder:
                    if (!HasAttribute(ToppingAttribute.Bitter))
                    {
                        Debug.Log($"[{toppingName}] 巧克力粉建议添加’苦’属性");
                    }
                    break;

                case ToppingType.StarfishSugar:
                    if (!HasAttribute(ToppingAttribute.Salty) && !HasAttribute(ToppingAttribute.Crispy))
                    {
                        Debug.Log($"[{toppingName}] 海星糖建议添加’咸’或’脆’属性");
                    }
                    break;

                case ToppingType.EyeballBoba:
                    if (!HasAttribute(ToppingAttribute.Boba) && !HasAttribute(ToppingAttribute.ExoticAroma))
                    {
                        Debug.Log($"[{toppingName}] 眼球爆珠建议添加’爆珠’或’异香’属性");
                    }
                    break;

                case ToppingType.MoonDust:
                    if (!HasAttribute(ToppingAttribute.ExoticAroma))
                    {
                        Debug.Log($"[{toppingName}] 月尘粉建议添加’异香’属性");
                    }
                    break;

                case ToppingType.BlackSalt:
                    if (!HasAttribute(ToppingAttribute.Salty) && !HasAttribute(ToppingAttribute.Bitter))
                    {
                        Debug.Log($"[{toppingName}] 黑盐建议添加’咸’或’苦’属性");
                    }
                    break;
            }
        }
    }
}
