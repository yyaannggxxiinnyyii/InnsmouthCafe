using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 收集物获得提示UI
    /// 显示获得的收集物信息，显示时暂停游戏时间，关闭后恢复
    /// </summary>
    public class CollectibleNotifyUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] [Tooltip("面板 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField] [Tooltip("收集物图标")]
        private Image _icon;

        [SerializeField] [Tooltip("收集物名称")]
        private TextMeshProUGUI _nameText;

        [SerializeField] [Tooltip("收集物描述")]
        private TextMeshProUGUI _descriptionText;

        [SerializeField] [Tooltip("确认按钮")]
        private Button _confirmButton;

        [Header("动画设置")]
        [SerializeField] [Tooltip("淡入时长")]
        private float _fadeInDuration = 0.3f;

        [SerializeField] [Tooltip("淡出时长")]
        private float _fadeOutDuration = 0.25f;

        private Action _onDismissCallback;

        private void Awake()
        {
            _confirmButton?.onClick.AddListener(OnConfirmClicked);
            SetGroupState(false);
        }

        /// <summary>
        /// 显示收集物获得提示
        /// </summary>
        /// <param name="collectible">获得的收集物</param>
        /// <param name="onDismiss">关闭后的回调</param>
        public void Show(CollectibleSO collectible, Action onDismiss)
        {
            if (collectible == null) return;

            _onDismissCallback = onDismiss;

            // 填充内容
            if (_icon != null)
                _icon.sprite = collectible.icon;
            if (_nameText != null)
                _nameText.text = collectible.collectibleName;
            if (_descriptionText != null)
                _descriptionText.text = collectible.description;

            // 暂停游戏时间
            Time.timeScale = 0f;

            // 淡入
            FadeIn();
        }

        private void OnConfirmClicked()
        {
            FadeOut(() =>
            {
                // 恢复游戏时间
                Time.timeScale = 1f;

                _onDismissCallback?.Invoke();
                _onDismissCallback = null;
            });
        }

        private void FadeIn()
        {
            if (_canvasGroup == null) return;

            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, _fadeInDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .OnComplete(() => _canvasGroup.interactable = true);
        }

        private void FadeOut(Action onComplete)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            _canvasGroup.DOKill();
            _canvasGroup.interactable = false;
            _canvasGroup.DOFade(0f, _fadeOutDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    onComplete?.Invoke();
                });
        }

        private void SetGroupState(bool visible)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
