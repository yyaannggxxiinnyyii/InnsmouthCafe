using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 资源图鉴条目UI，负责显示辅助液或小料的图标与名称，并在未解锁时显示占位图标。
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

        [Header("锁定状态")]
        [SerializeField]
        [Tooltip("资源未解锁时显示的占位图标")]
        private Sprite _lockedIconSprite;

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
            if (!isUnlocked)
            {
                SetLockedState();
                return;
            }

            SetText(_nameText, itemName);
            SetSprite(_iconImage, icon);
        }

        /// <summary>
        /// 显示未解锁状态。
        /// </summary>
        private void SetLockedState()
        {
            SetText(_nameText, "???");
            SetSprite(_iconImage, _lockedIconSprite);
        }

        /// <summary>
        /// 设置文本内容。
        /// </summary>
        private void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        /// <summary>
        /// 设置图片内容，并在图片为空时隐藏 Image。
        /// </summary>
        private void SetSprite(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
