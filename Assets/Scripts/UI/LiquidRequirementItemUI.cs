using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 辅助液需求项UI
    /// 挂载在辅助液需求项预制体上，显示液体图标+容量
    /// </summary>
    public class LiquidRequirementItemUI : MonoBehaviour
    {
        [SerializeField] [Tooltip("辅助液图标")]
        private Image _liquidIcon;

        [SerializeField] [Tooltip("容量文本")]
        private TextMeshProUGUI _volumeText;

        /// <summary>
        /// 初始化辅助液需求项显示
        /// </summary>
        public void Init(LiquidRequirementData data)
        {
            if (data.liquid != null && _liquidIcon != null)
            {
                _liquidIcon.sprite = data.liquid.icon;
            }

            if (_volumeText != null)
            {
                _volumeText.text = $"{data.targetVolume}ml";
            }
        }
    }
}
