using System;
using InnsmouthCafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 角色图鉴详情面板UI，负责显示已遇见角色的介绍、特性，并通过立绘按钮打开立绘详情面板。
    /// </summary>
    public class GalleryCharacterDetailPanelUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField]
        [Tooltip("角色详情面板 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        [Tooltip("关闭角色详情面板按钮")]
        private Button _closeButton;

        [Header("角色信息")]
        [SerializeField]
        [Tooltip("角色立绘按钮")]
        private Button _portraitButton;

        [SerializeField]
        [Tooltip("角色立绘图片")]
        private Image _portraitImage;

        [SerializeField]
        [Tooltip("角色名称文本")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("角色描述文本")]
        private TextMeshProUGUI _descriptionText;

        [SerializeField]
        [Tooltip("角色特性介绍文本")]
        private TextMeshProUGUI _effectText;

        private CustomerSO _currentCustomer;
        private bool _isPerfected;
        private Action<CustomerSO, bool> _onPortraitClicked;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            _portraitButton?.onClick.AddListener(HandlePortraitClicked);
            Hide();
        }

        /// <summary>
        /// 显示角色详情，并缓存打开立绘详情面板所需的回调。
        /// </summary>
        public void Show(CustomerSO customer, bool isPerfected, Action<CustomerSO, bool> onPortraitClicked)
        {
            _currentCustomer = customer;
            _isPerfected = isPerfected;
            _onPortraitClicked = onPortraitClicked;

            if (customer == null)
            {
                Hide();
                return;
            }

            SetText(_nameText, customer.customerName);
            SetText(_descriptionText, customer.galleryCharacterDescription);
            SetText(_effectText, customer.galleryEffectDescription);
            SetSprite(_portraitImage, customer.normalSprite);
            SetButtonInteractable(_portraitButton, customer.normalSprite != null);
            SetCanvasGroupVisible(_canvasGroup, true);
        }

        /// <summary>
        /// 隐藏角色详情面板。
        /// </summary>
        public void Hide()
        {
            SetCanvasGroupVisible(_canvasGroup, false);
        }

        /// <summary>
        /// 处理角色立绘点击，打开角色立绘详情面板。
        /// </summary>
        private void HandlePortraitClicked()
        {
            if (_currentCustomer == null)
            {
                return;
            }

            _onPortraitClicked?.Invoke(_currentCustomer, _isPerfected);
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
        /// 设置按钮可交互状态。
        /// </summary>
        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
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
