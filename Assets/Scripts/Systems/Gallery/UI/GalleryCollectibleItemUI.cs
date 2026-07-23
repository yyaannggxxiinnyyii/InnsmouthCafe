using System;
using InnsmouthCafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 收集物图鉴预览条目UI，负责显示收集物图标与名称，并在已获得时通知外部打开详情面板。
    /// </summary>
    public class GalleryCollectibleItemUI : MonoBehaviour
    {
        [Header("基础显示")]
        [SerializeField]
        [Tooltip("收集物名称文本")]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        [Tooltip("收集物图标")]
        private Image _iconImage;

        [SerializeField]
        [Tooltip("点击打开收集物详情的按钮")]
        private Button _openDetailButton;

        [Header("牌背状态")]
        [SerializeField]
        [Tooltip("未解锁时遮挡牌面的牌背 CanvasGroup")]
        private CanvasGroup _cardBackCanvasGroup;

        private CollectibleSO _currentCollectible;
        private bool _isObtained;
        private Action<CollectibleSO> _onSelected;

        private void Awake()
        {
            _openDetailButton?.onClick.AddListener(HandleOpenDetailClicked);
        }

        /// <summary>
        /// 使用运行时生成的控件引用，供没有预制体绑定时兜底创建条目。
        /// </summary>
        public void UseRuntimeReferences(
            TextMeshProUGUI nameText,
            Image iconImage,
            Button openDetailButton)
        {
            _nameText = nameText;
            _iconImage = iconImage;
            _openDetailButton = openDetailButton;
            _openDetailButton?.onClick.AddListener(HandleOpenDetailClicked);
        }

        /// <summary>
        /// 根据收集物获得状态刷新预览条目显示，并缓存点击回调。
        /// </summary>
        public void Configure(
            CollectibleSO collectible,
            bool isObtained,
            Action<CollectibleSO> onSelected)
        {
            _currentCollectible = collectible;
            _isObtained = isObtained;
            _onSelected = onSelected;

            SetCardBackVisible(!isObtained);
            SetButtonInteractable(isObtained);

            if (collectible == null)
            {
                return;
            }

            if (_nameText != null)
            {
                _nameText.text = collectible.collectibleName;
            }

            if (_iconImage != null)
            {
                _iconImage.sprite = collectible.icon;
                _iconImage.enabled = collectible.icon != null;
            }
        }

        /// <summary>
        /// 处理预览条目点击，通知图鉴面板打开对应收集物详情。
        /// </summary>
        private void HandleOpenDetailClicked()
        {
            if (_currentCollectible == null || !_isObtained)
            {
                return;
            }

            _onSelected?.Invoke(_currentCollectible);
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
