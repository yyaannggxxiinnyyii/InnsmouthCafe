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
    /// 作为主菜单子面板，展示普通和困难游戏模式
    /// 未开放模式保留显示并禁用交互
    /// </summary>
    public class ModeSelectPanelUI : MonoBehaviour
    {
        [Header("CanvasGroup")]
        [SerializeField] [Tooltip("本面板的 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [Header("模式按钮")]
        [SerializeField] [Tooltip("普通模式按钮")]
        private Button _normalButton;

        [SerializeField] [Tooltip("困难模式按钮")]
        private Button _hardButton;

        [SerializeField] [Tooltip("无尽模式按钮")]
        private Button _endlessButton;

        [Header("模式配置")]
        [SerializeField] [Tooltip("普通模式配置SO")]
        private GameModeConfigSO _normalConfig;

        [SerializeField] [Tooltip("困难模式配置SO")]
        private GameModeConfigSO _hardConfig;

        [Header("底部按钮")]
        [SerializeField] [Tooltip("返回主菜单按钮")]
        private Button _backButton;

        [Header("过渡设置")]
        [SerializeField] [Tooltip("面板淡入淡出时长")]
        private float _fadeDuration = 0.25f;

        private MainMenuUI _mainMenu;

        private const string NormalUnlockTip = "完成新手教程后解锁";
        private const string HardUnlockTip = "在普通模式中达成回归结局或好结局后解锁";
        private const string EndlessUnlockTip = "累计收集迷失、回归、好结局后解锁";
        private const string EndlessDevelopmentTip = "已满足解锁条件，无尽模式开发中";

        private void Awake()
        {
            _mainMenu = FindObjectOfType<MainMenuUI>();

            _normalButton?.onClick.AddListener(OnNormalClicked);
            _hardButton?.onClick.AddListener(OnHardClicked);
            _backButton?.onClick.AddListener(OnBackClicked);

            EnsureTooltipSystem();
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
            TooltipSystem.Instance?.Hide();
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
            bool hardUnlocked = GameManager.Instance != null
                && GameManager.Instance.IsModeUnlocked(GameMode.Hard);
            bool endlessConditionMet = GameManager.Instance != null
                && GameManager.Instance.IsModeUnlocked(GameMode.Endless);

            SetButtonInteractable(_normalButton, normalUnlocked);
            SetButtonInteractable(_hardButton, hardUnlocked);
            SetButtonInteractable(_endlessButton, false);

            ConfigureUnlockTooltip(_normalButton, !normalUnlocked, "普通模式", NormalUnlockTip);
            ConfigureUnlockTooltip(_hardButton, !hardUnlocked, "困难模式", HardUnlockTip);
            ConfigureUnlockTooltip(
                _endlessButton,
                true,
                "无尽模式",
                endlessConditionMet ? EndlessDevelopmentTip : EndlessUnlockTip);
        }

        /// <summary>确保主菜单 Canvas 中存在通用 Tooltip 实例。</summary>
        private void EnsureTooltipSystem()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            TextMeshProUGUI fontSource = _normalButton != null
                ? _normalButton.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            TooltipSystem.EnsureForCanvas(canvas, fontSource != null ? fontSource.font : null);
        }

        /// <summary>配置模式按钮的悬停解锁说明。</summary>
        private void ConfigureUnlockTooltip(
            Button button,
            bool shouldShow,
            string title,
            string description)
        {
            if (button == null)
            {
                return;
            }

            HoverTooltipTrigger trigger = button.GetComponent<HoverTooltipTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<HoverTooltipTrigger>();
            }

            trigger.ConfigureText(title, description);
            trigger.enabled = shouldShow;
        }

        /// <summary>设置模式按钮是否允许点击。</summary>
        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
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

        /// <summary>使用困难模式配置启动新游戏。</summary>
        private void OnHardClicked()
        {
            if (_hardConfig == null)
            {
                Debug.LogError("[ModeSelect] 困难模式配置未设置");
                return;
            }

            GameManager.Instance?.StartGameWithConfig(_hardConfig);
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
