using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 资源图鉴条目UI，负责显示辅助液或小料的图标与名称，并通过牌背遮挡未解锁内容。
    /// </summary>
    public class GalleryIngredientItemUI : MonoBehaviour
    {
        [Header("基础显示")]
        [SerializeField]
        [Tooltip("资源名称文本")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("资源图标")]
        private Image _iconImage;

        [SerializeField]
        [Tooltip("使用资源图标轮廓显示浮雕效果的覆盖层")]
        private Image _iconEmbossImage;

        [Header("牌背状态")]
        [SerializeField]
        [Tooltip("未解锁时遮挡牌面的牌背 CanvasGroup")]
        private CanvasGroup _cardBackCanvasGroup;

        /// <summary>
        /// 使用运行时生成的控件引用，供没有预制体绑定时兜底创建条目。
        /// </summary>
        public void UseRuntimeReferences(TextMeshProUGUI nameText, Image iconImage)
        {
            _nameText = nameText;
            _iconImage = iconImage;
        }

        /// <summary>
        /// 根据资源解锁状态刷新条目显示。
        /// </summary>
        public void Configure(string itemName, Sprite icon, bool isUnlocked)
        {
            SetCardBackVisible(!isUnlocked);

            if (_nameText != null)
            {
                _nameText.text = itemName;
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }

            if (_iconEmbossImage != null)
            {
                _iconEmbossImage.sprite = icon;
                _iconEmbossImage.enabled = icon != null;
            }
        }

        /// <summary>
        /// 设置牌背显隐及射线遮挡状态。
        /// </summary>
        private void SetCardBackVisible(bool visible)
        {
            if (_cardBackCanvasGroup == null)
            {
                return;
            }

            _cardBackCanvasGroup.alpha = visible ? 1f : 0f;
            _cardBackCanvasGroup.interactable = visible;
            _cardBackCanvasGroup.blocksRaycasts = visible;
        }
    }
}
