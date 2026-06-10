using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 单个材料解锁提示条目，负责显示材料图标、名称和类型。
    /// </summary>
    public class IngredientUnlockNotifyItemUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField]
        [Tooltip("材料图标")]
        private Image _icon;

        [SerializeField]
        [Tooltip("材料名称文本")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("材料类型文本")]
        private TextMeshProUGUI _typeText;

        /// <summary>
        /// 配置材料解锁条目显示内容。
        /// </summary>
        public void Configure(Sprite icon, string itemName, string itemType)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }

            if (_nameText != null)
            {
                _nameText.text = itemName;
            }

            if (_typeText != null)
            {
                _typeText.text = itemType;
            }
        }
    }
}
