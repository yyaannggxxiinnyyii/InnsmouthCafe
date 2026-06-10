using InnsmouthCafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 收集物图鉴详情面板UI，负责显示已获得收集物的名称、图标和说明文本。
    /// </summary>
    public class GalleryCollectibleDetailPanelUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField]
        [Tooltip("收集物详情面板 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        [Tooltip("关闭收集物详情面板按钮")]
        private Button _closeButton;

        [Header("收集物信息")]
        [SerializeField]
        [Tooltip("收集物图标")]
        private Image _iconImage;

        [SerializeField]
        [Tooltip("收集物名称文本")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("收集物描述文本")]
        private TextMeshProUGUI _descriptionText;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            Hide();
        }

        /// <summary>
        /// 使用运行时生成的控件引用，供没有预制体绑定时兜底创建详情面板。
        /// </summary>
        public void UseRuntimeReferences(
            CanvasGroup canvasGroup,
            Button closeButton,
            Image iconImage,
            TextMeshProUGUI nameText,
            TextMeshProUGUI descriptionText)
        {
            _canvasGroup = canvasGroup;
            _closeButton = closeButton;
            _iconImage = iconImage;
            _nameText = nameText;
            _descriptionText = descriptionText;
            _closeButton?.onClick.AddListener(Hide);
            Hide();
        }

        /// <summary>
        /// 显示收集物详情。
        /// </summary>
        public void Show(CollectibleSO collectible)
        {
            if (collectible == null)
            {
                Hide();
                return;
            }

            SetText(_nameText, collectible.collectibleName);
            SetText(_descriptionText, collectible.description);
            SetSprite(_iconImage, collectible.icon);
            SetCanvasGroupVisible(_canvasGroup, true);
        }

        /// <summary>
        /// 隐藏收集物详情面板。
        /// </summary>
        public void Hide()
        {
            SetCanvasGroupVisible(_canvasGroup, false);
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

        /// <summary>
        /// 通过 CanvasGroup 设置面板显隐与交互。
        /// </summary>
        private void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
