using System;
using InnsmouthCafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 角色图鉴预览条目UI，负责显示角色立绘与名称，并在可查看时通知外部打开详情面板。
    /// </summary>
    public class GalleryCharacterItemUI : MonoBehaviour
    {
        [Header("基础显示")]
        [SerializeField]
        [Tooltip("角色名称文本")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("角色预览立绘")]
        private Image _portraitImage;

        [SerializeField]
        [Tooltip("点击打开角色详情的按钮")]
        private Button _openDetailButton;

        [Header("锁定状态")]
        [SerializeField]
        [Tooltip("角色未遇见时显示的占位立绘")]
        private Sprite _lockedPortraitSprite;

        private CustomerSO _currentCustomer;
        private bool _isEncountered;
        private bool _isPerfected;
        private Action<CustomerSO, bool> _onSelected;

        private void Awake()
        {
            _openDetailButton?.onClick.AddListener(HandleOpenDetailClicked);
        }

        /// <summary>
        /// 根据角色图鉴状态刷新预览条目显示，并缓存点击回调。
        /// </summary>
        public void Configure(
            CustomerSO customer,
            bool isEncountered,
            bool isPerfected,
            Action<CustomerSO, bool> onSelected)
        {
            _currentCustomer = customer;
            _isEncountered = isEncountered;
            _isPerfected = isPerfected;
            _onSelected = onSelected;

            if (customer == null || !isEncountered)
            {
                SetLockedState();
                return;
            }

            SetText(_nameText, customer.customerName);
            SetSprite(_portraitImage, customer.normalSprite);
            SetButtonInteractable(true);
        }

        /// <summary>
        /// 显示未遇见状态，并禁止打开角色详情。
        /// </summary>
        private void SetLockedState()
        {
            SetText(_nameText, "???");
            SetSprite(_portraitImage, _lockedPortraitSprite);
            SetButtonInteractable(false);
        }

        /// <summary>
        /// 处理预览条目点击，通知图鉴面板打开对应角色详情。
        /// </summary>
        private void HandleOpenDetailClicked()
        {
            if (_currentCustomer == null || !_isEncountered)
            {
                return;
            }

            _onSelected?.Invoke(_currentCustomer, _isPerfected);
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
        private void SetButtonInteractable(bool interactable)
        {
            if (_openDetailButton != null)
            {
                _openDetailButton.interactable = interactable;
            }
        }

    }
}
