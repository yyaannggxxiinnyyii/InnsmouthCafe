using System;
using InnsmouthCafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 角色图鉴预览条目UI，负责显示角色立绘与名称，并通过牌背遮挡未遇见角色。
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
        [Tooltip("使用角色立绘轮廓显示镭射效果的覆盖层")]
        private Image _portraitEffectImage;

        [SerializeField]
        [Tooltip("使用角色立绘轮廓显示浮雕效果的覆盖层")]
        private Image _portraitEmbossImage;

        [SerializeField]
        [Tooltip("点击打开角色详情的按钮")]
        private Button _openDetailButton;

        [Header("牌背状态")]
        [SerializeField]
        [Tooltip("未遇见时遮挡牌面的牌背 CanvasGroup")]
        private CanvasGroup _cardBackCanvasGroup;

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

            SetCardBackVisible(!isEncountered);
            SetButtonInteractable(isEncountered);

            if (customer == null)
            {
                return;
            }

            if (_nameText != null)
            {
                _nameText.text = customer.customerName;
            }

            Sprite portraitSprite = customer.normalSprite;
            if (_portraitImage != null)
            {
                _portraitImage.sprite = portraitSprite;
            }

            if (_portraitEffectImage != null)
            {
                _portraitEffectImage.sprite = portraitSprite;
            }

            if (_portraitEmbossImage != null)
            {
                _portraitEmbossImage.sprite = portraitSprite;
            }
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
