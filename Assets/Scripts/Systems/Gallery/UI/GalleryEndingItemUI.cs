using System;
using InnsmouthCafe.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 结局图鉴条目UI，负责显示已收集结局的图标、名称和详细查看入口。
    /// </summary>
    public class GalleryEndingItemUI : MonoBehaviour
    {
        [Header("基础显示")]
        [SerializeField]
        [Tooltip("结局图标")]
        private Image _endingIconImage;

        [SerializeField]
        [Tooltip("结局名称文本")]
        private TextMeshProUGUI _endingNameText;

        [SerializeField]
        [Tooltip("详细查看按钮")]
        private Button _detailButton;

        private GameEnding _currentEnding;
        private Action<GameEnding> _onDetailClicked;

        private void Awake()
        {
            _detailButton?.onClick.AddListener(HandleDetailClicked);
        }

        /// <summary>
        /// 使用运行时生成的控件引用，供没有预制体绑定时兜底创建条目。
        /// </summary>
        public void UseRuntimeReferences(Image endingIconImage, TextMeshProUGUI endingNameText, Button detailButton)
        {
            _endingIconImage = endingIconImage;
            _endingNameText = endingNameText;
            _detailButton = detailButton;
            _detailButton?.onClick.AddListener(HandleDetailClicked);
        }

        /// <summary>
        /// 根据结局配置刷新条目显示，并缓存详细查看回调。
        /// </summary>
        public void Configure(
            GameEnding ending,
            EndingConfig endingConfig,
            bool canViewDetail,
            Action<GameEnding> onDetailClicked)
        {
            _currentEnding = ending;
            _onDetailClicked = onDetailClicked;

            if (endingConfig == null)
            {
                SetText(_endingNameText, string.Empty);
                SetSprite(_endingIconImage, null);
                SetButtonInteractable(false);
                return;
            }

            SetText(_endingNameText, endingConfig.endingTitle);
            SetSprite(_endingIconImage, endingConfig.endingImage);
            SetButtonInteractable(canViewDetail);
        }

        /// <summary>
        /// 处理详细查看按钮点击。
        /// </summary>
        private void HandleDetailClicked()
        {
            if (_detailButton != null && !_detailButton.interactable)
            {
                return;
            }

            _onDetailClicked?.Invoke(_currentEnding);
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
        /// 设置详细按钮可交互状态。
        /// </summary>
        private void SetButtonInteractable(bool interactable)
        {
            if (_detailButton != null)
            {
                _detailButton.interactable = interactable;
            }
        }
    }
}
