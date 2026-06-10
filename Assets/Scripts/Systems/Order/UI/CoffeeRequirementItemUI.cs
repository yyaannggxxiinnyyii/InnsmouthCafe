using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 咖啡液需求项UI
    /// 挂载在咖啡液需求项预制体上，显示豆图标+研磨度+容量
    /// </summary>
    public class CoffeeRequirementItemUI : MonoBehaviour
    {
        [SerializeField] [Tooltip("咖啡豆图标")]
        private Image _beanIcon;

        [SerializeField] [Tooltip("研磨度文本")]
        private TextMeshProUGUI _grindText;

        [SerializeField] [Tooltip("容量文本")]
        private TextMeshProUGUI _volumeText;

        /// <summary>
        /// 初始化咖啡液需求项显示
        /// </summary>
        public void Init(CoffeeRequirementData data)
        {
            if (data.bean != null && _beanIcon != null)
            {
                _beanIcon.sprite = data.bean.icon;
            }

            if (_grindText != null)
            {
                _grindText.text = GetGrindTypeName(data.grindType);
            }

            if (_volumeText != null)
            {
                _volumeText.text = $"{data.targetVolume/5}g";
            }
        }

        /// <summary>
        /// 获取研磨度名称
        /// </summary>
        private string GetGrindTypeName(GrindType grindType)
        {
            switch (grindType)
            {
                case GrindType.Coarse: return "低精度研磨";
                case GrindType.Fine: return "中精度研磨";
                case GrindType.ExtraFine: return "高精度研磨";
                default: return "未知";
            }
        }
    }
}
