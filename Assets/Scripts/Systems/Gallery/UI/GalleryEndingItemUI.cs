using System;
using InnsmouthCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 结局图鉴固定槽位UI，负责切换解锁状态并提供结局回放入口。
    /// </summary>
    public class GalleryEndingItemUI : MonoBehaviour
    {
        [Header("基础显示")]
        [SerializeField]
        [Tooltip("结局图像；未解锁时保持黑色占位，解锁后显示结局图片")]
        private Image _endingImage;

        [SerializeField]
        [Tooltip("未解锁提示节点")]
        private GameObject _lockedHint;

        [SerializeField]
        [Tooltip("覆盖整个结局槽位的回放按钮")]
        private Button _replayButton;

        private GameEnding _currentEnding;
        private Action<GameEnding> _onReplayClicked;
        private Color _lockedImageColor = Color.black;

        private void Awake()
        {
            if (_endingImage != null)
            {
                _lockedImageColor = _endingImage.color;
            }

            _replayButton?.onClick.AddListener(HandleReplayClicked);
        }

        private void OnDestroy()
        {
            _replayButton?.onClick.RemoveListener(HandleReplayClicked);
        }

        /// <summary>
        /// 根据结局解锁状态刷新固定槽位，并缓存回放回调。
        /// </summary>
        public void Configure(
            GameEnding ending,
            EndingConfig endingConfig,
            bool isUnlocked,
            bool canReplay,
            Action<GameEnding> onReplayClicked)
        {
            _currentEnding = ending;
            _onReplayClicked = onReplayClicked;

            bool showUnlocked = isUnlocked && endingConfig != null;
            if (_endingImage != null)
            {
                _endingImage.sprite = showUnlocked ? endingConfig.endingImage : null;
                _endingImage.color = showUnlocked ? Color.white : _lockedImageColor;
            }

            if (_lockedHint != null)
            {
                _lockedHint.SetActive(!showUnlocked);
            }

            if (_replayButton != null)
            {
                _replayButton.interactable = showUnlocked && canReplay;
            }
        }

        /// <summary>
        /// 处理结局槽位点击。
        /// </summary>
        private void HandleReplayClicked()
        {
            if (_replayButton != null && !_replayButton.interactable)
            {
                return;
            }

            _onReplayClicked?.Invoke(_currentEnding);
        }
    }
}
