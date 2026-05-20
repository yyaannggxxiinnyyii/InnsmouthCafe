using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 小料需求项UI
    /// 挂载在小料需求项预制体上，显示小料图标+名称
    /// </summary>
    public class ToppingRequirementItemUI : MonoBehaviour
    {
        [SerializeField] [Tooltip("小料图标")]
        private Image _toppingIcon;

        [SerializeField] [Tooltip("名称文本")]
        private TextMeshProUGUI _nameText;

        /// <summary>
        /// 用指定小料初始化
        /// </summary>
        public void Init(ToppingSO topping)
        {
            if (topping != null && _toppingIcon != null)
            {
                _toppingIcon.sprite = topping.icon;
            }

            if (_nameText != null)
            {
                _nameText.text = topping != null ? topping.toppingName : "";
            }
        }

        /// <summary>
        /// 用小料标签初始化
        /// </summary>
        public void Init(ToppingTagSO tag)
        {
            if (tag != null && _toppingIcon != null)
            {
                _toppingIcon.sprite = tag.icon;
            }

            if (_nameText != null)
            {
                _nameText.text = tag != null ? tag.tagName : "";
            }
        }
    }
}
