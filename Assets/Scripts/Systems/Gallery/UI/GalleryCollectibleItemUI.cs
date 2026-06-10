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

        [Header("锁定状态")]
        [SerializeField]
        [Tooltip("收集物未获得时显示的占位图标")]
        private Sprite _lockedIconSprite;

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

            if (collectible == null || !isObtained)
            {
                SetLockedState();
                return;
            }

            SetText(_nameText, collectible.collectibleName);
            SetSprite(_iconImage, collectible.icon);
            SetButtonInteractable(true);
        }

        /// <summary>
        /// 显示未获得状态，并禁止打开收集物详情。
        /// </summary>
        private void SetLockedState()
        {
            SetText(_nameText, "???");
            SetSprite(_iconImage, _lockedIconSprite);
            SetButtonInteractable(false);
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
