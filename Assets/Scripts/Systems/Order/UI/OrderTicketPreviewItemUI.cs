using System;
using UnityEngine;
using UnityEngine.UI;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 订单小票缩略项UI，负责显示订单者头像、选中态和完成态，并处理选中后再次点击打开详情的交互。
    /// </summary>
    public class OrderTicketPreviewItemUI : MonoBehaviour
    {
        [Header("交互")]
        [SerializeField]
        [Tooltip("点击后选择或打开该订单槽的按钮；为空时自动使用自身 Button")]
        private Button _selectButton;

        [Header("图像")]
        [SerializeField]
        [Tooltip("订单者头像图片")]
        private Image _participantAvatarImage;

        [SerializeField]
        [Tooltip("选中状态图片")]
        private Image _selectedMarker;

        [SerializeField]
        [Tooltip("订单品质图标显示图片")]
        private Image _completedMarker;

        [Header("订单品质图标")]
        [SerializeField]
        [Tooltip("糟糕品质图标，对应 Terrible")]
        private Sprite _terribleQualityIcon;

        [SerializeField]
        [Tooltip("合格品质图标，对应 Acceptable")]
        private Sprite _acceptableQualityIcon;

        [SerializeField]
        [Tooltip("完美品质图标，对应 Perfect")]
        private Sprite _perfectQualityIcon;

        /// <summary>
        /// 小票缩略项被选择时触发。
        /// </summary>
        public event Action<OrderTicketPreviewItemUI> OnSelected;

        /// <summary>
        /// 小票缩略项请求打开详情时触发。
        /// </summary>
        public event Action<OrderTicketPreviewItemUI> OnDetailRequested;

        /// <summary>
        /// 当前绑定的订单槽。
        /// </summary>
        public CustomerOrderSlotData BoundSlot { get; private set; }

        /// <summary>
        /// 当前缩略项是否处于选中状态。
        /// </summary>
        public bool IsSelected { get; private set; }

        private void Awake()
        {
            if (_selectButton == null)
            {
                _selectButton = GetComponent<Button>();
            }

            _selectButton?.onClick.AddListener(HandleSelectClicked);
        }

        private void OnDestroy()
        {
            _selectButton?.onClick.RemoveListener(HandleSelectClicked);
        }

        /// <summary>
        /// 使用订单槽数据刷新缩略项显示。
        /// </summary>
        /// <param name="slot">需要绑定的订单槽。</param>
        /// <param name="selected">是否为当前选中项。</param>
        public void Bind(CustomerOrderSlotData slot, bool selected)
        {
            BoundSlot = slot;
            RefreshAvatar();
            RefreshState();
            SetSelected(selected);
        }

        /// <summary>
        /// 设置小票缩略项选中状态。
        /// </summary>
        /// <param name="selected">是否选中。</param>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            if (_selectedMarker != null)
            {
                _selectedMarker.gameObject.SetActive(selected);
            }
        }

        /// <summary>
        /// 刷新订单品质图标。
        /// </summary>
        public void RefreshState()
        {
            if (_selectButton != null)
            {
                _selectButton.interactable = BoundSlot != null
                    && BoundSlot.IsWaitingForSubmission;
            }

            if (_completedMarker == null)
            {
                return;
            }

            bool hasScoringResult = BoundSlot != null
                && BoundSlot.scoringData != null
                && BoundSlot.state == CustomerOrderSlotState.Completed;
            _completedMarker.gameObject.SetActive(hasScoringResult);

            if (!hasScoringResult)
            {
                return;
            }

            Sprite qualityIcon = GetQualityIcon(BoundSlot.scoringData.qualityLevel);
            if (qualityIcon != null)
            {
                _completedMarker.sprite = qualityIcon;
            }
        }

        /// <summary>
        /// 处理小票按钮点击，未选中时先选中，已选中时打开详情。
        /// </summary>
        private void HandleSelectClicked()
        {
            if (BoundSlot == null)
            {
                return;
            }

            if (IsSelected)
            {
                OnDetailRequested?.Invoke(this);
                return;
            }

            OnSelected?.Invoke(this);
        }

        /// <summary>
        /// 刷新订单者头像显示。
        /// </summary>
        private void RefreshAvatar()
        {
            if (_participantAvatarImage == null)
            {
                return;
            }

            Sprite avatarSprite = BoundSlot?.GetOrdererAvatar();
            _participantAvatarImage.sprite = avatarSprite;
            _participantAvatarImage.enabled = avatarSprite != null;
        }

        /// <summary>
        /// 根据订单品质获取对应的小票品质图标。
        /// </summary>
        /// <param name="quality">订单评分品质。</param>
        /// <returns>配置过的品质图标；未配置时返回当前图片原图作为兜底。</returns>
        private Sprite GetQualityIcon(CoffeeQuality quality)
        {
            Sprite icon = quality switch
            {
                CoffeeQuality.Perfect => _perfectQualityIcon,
                CoffeeQuality.Acceptable => _acceptableQualityIcon,
                CoffeeQuality.Terrible => _terribleQualityIcon,
                _ => null
            };

            return icon != null
                ? icon
                : _completedMarker.sprite;
        }
    }
}
