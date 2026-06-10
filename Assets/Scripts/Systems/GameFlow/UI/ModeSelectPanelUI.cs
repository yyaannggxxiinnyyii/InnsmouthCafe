using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 模式选择面板UI
    /// 作为主菜单子面板，展示可选游戏模式（普通）
    /// 未解锁模式灰显并显示锁图标
    /// </summary>
    public class ModeSelectPanelUI : MonoBehaviour
    {
        [Header("CanvasGroup")]
        [SerializeField] [Tooltip("本面板的 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [Header("模式按钮")]
        [SerializeField] [Tooltip("普通模式按钮")]
        private Button _normalButton;

        [Header("锁图标")]
        [SerializeField] [Tooltip("普通模式锁图标（未解锁时显示）")]
        private GameObject _normalLock;

        [Header("按钮文本")]
        [SerializeField] [Tooltip("普通模式按钮文本")]
        private TextMeshProUGUI _normalText;

        [Header("模式配置")]
        [SerializeField] [Tooltip("普通模式配置SO")]
        private GameModeConfigSO _normalConfig;

        [Header("底部按钮")]
        [SerializeField] [Tooltip("返回主菜单按钮")]
        private Button _backButton;

        [Header("过渡设置")]
        [SerializeField] [Tooltip("面板淡入淡出时长")]
        private float _fadeDuration = 0.25f;

        [Header("锁定状态颜色")]
        [SerializeField] [Tooltip("按钮锁定时的颜色")]
        private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        [SerializeField] [Tooltip("按钮解锁时的颜色")]
        private Color _unlockedColor = Color.white;

        private MainMenuUI _mainMenu;

        private void Awake()
        {
            _mainMenu = FindObjectOfType<MainMenuUI>();

            _normalButton?.onClick.AddListener(OnNormalClicked);
            _backButton?.onClick.AddListener(OnBackClicked);
        }

        // ── 显示/隐藏 ─────────────────────────────────────────

        /// <summary>显示面板并刷新解锁状态</summary>
        public void Show()
        {
            RefreshModeStates();
            FadePanel(true);
        }

        /// <summary>隐藏面板</summary>
        public void Hide(bool immediate = false)
        {
            if (immediate)
                SetGroupState(_canvasGroup, false);
            else
                FadePanel(false);
        }

        // ── 刷新解锁状态 ────────────────────────────────────────

        /// <summary>根据 GameManager 的解锁数据刷新按钮状态</summary>
        private void RefreshModeStates()
        {
            bool normalUnlocked = GameManager.Instance != null
                && GameManager.Instance.IsModeUnlocked(GameMode.Normal);

            SetButtonState(_normalButton, _normalLock, _normalText, normalUnlocked);
        }

        /// <summary>设置单个模式按钮的可交互/锁定状态</summary>
        private void SetButtonState(Button button, GameObject lockIcon, TextMeshProUGUI text, bool unlocked)
        {
            if (button != null)
            {
                button.interactable = unlocked;

                var buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.color = unlocked ? _unlockedColor : _lockedColor;
            }

            if (lockIcon != null)
                lockIcon.SetActive(!unlocked);

            if (text != null)
                text.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        // ── 按钮回调 ────────────────────────────────────────────

        private void OnNormalClicked()
        {
            if (_normalConfig == null)
            {
                Debug.LogError("[ModeSelect] 普通模式配置未设置");
                return;
            }

            GameManager.Instance?.StartGameWithConfig(_normalConfig);
        }

        private void OnBackClicked()
        {
            _mainMenu?.OnModeSelectClosed();
        }

        // ── CanvasGroup 工具 ────────────────────────────────────

        private void FadePanel(bool fadeIn)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.DOKill();

            if (fadeIn)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.DOFade(1f, _fadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => _canvasGroup.interactable = true);
            }
            else
            {
                _canvasGroup.interactable = false;
                _canvasGroup.DOFade(0f, _fadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => _canvasGroup.blocksRaycasts = false);
            }
        }

        private void SetGroupState(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
