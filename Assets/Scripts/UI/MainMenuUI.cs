using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 主菜单UI管理器
    /// 使用 CanvasGroup + DOTween 在主菜单页和设置页之间淡入淡出切换
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("主菜单 CanvasGroup")]
        [SerializeField] [Tooltip("主菜单页面的 CanvasGroup")]
        private CanvasGroup _mainMenuGroup;

        [Header("按钮")]
        [SerializeField] [Tooltip("继续游戏按钮（无存档时隐藏）")]
        private Button _continueButton;

        [SerializeField] [Tooltip("开始/新游戏按钮")]
        private Button _startButton;

        [SerializeField] [Tooltip("设置按钮")]
        private Button _settingsButton;

        [SerializeField] [Tooltip("图鉴按钮（占位）")]
        private Button _codexButton;

        [SerializeField] [Tooltip("退出游戏按钮")]
        private Button _quitButton;

        [Header("按钮文本")]
        [SerializeField] [Tooltip("开始按钮的文本组件")]
        private TextMeshProUGUI _startButtonText;

        [SerializeField] [Tooltip("有存档时开始按钮显示的文本")]
        private string _newGameText = "新的游戏";

        [SerializeField] [Tooltip("无存档时开始按钮显示的文本")]
        private string _startGameText = "开始游戏";

        [Header("设置面板")]
        [SerializeField] [Tooltip("设置面板 UI 脚本（用于打开/关闭回调）")]
        private SettingsPanelUI _settingsPanel;

        [Header("模式选择面板")]
        [SerializeField] [Tooltip("模式选择面板 UI 脚本")]
        private ModeSelectPanelUI _modeSelectPanel;

        [Header("教学模式配置")]
        [SerializeField] [Tooltip("教学模式配置SO（首次游戏直接使用）")]
        private GameModeConfigSO _tutorialConfig;

        [Header("过渡设置")]
        [SerializeField] [Tooltip("页面切换淡入淡出时长")]
        private float _fadeDuration = 0.25f;

        private void Start()
        {
            BindButtons();
            RefreshButtonStates();

            // 确保主菜单初始可见，设置面板初始不可交互
            ShowGroup(_mainMenuGroup, true);
            if (_settingsPanel != null)
                _settingsPanel.Hide(immediate: true);
            if (_modeSelectPanel != null)
                _modeSelectPanel.Hide(immediate: true);
        }

        // ── 按钮绑定 ──────────────────────────────────────────

        private void BindButtons()
        {
            _continueButton?.onClick.AddListener(OnContinueClicked);
            _startButton?.onClick.AddListener(OnStartClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _codexButton?.onClick.AddListener(OnCodexClicked);
            _quitButton?.onClick.AddListener(OnQuitClicked);
        }

        // ── 存档状态 ──────────────────────────────────────────

        private void RefreshButtonStates()
        {
            bool hasSave = GameManager.Instance != null && GameManager.Instance.HasSaveData;

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(hasSave);

            if (_startButtonText != null)
                _startButtonText.text = hasSave ? _newGameText : _startGameText;
        }

        // ── 按钮回调 ──────────────────────────────────────────

        private void OnContinueClicked()
        {
            GameManager.Instance?.ContinueGame();
        }

        private void OnStartClicked()
        {
            // 教学模式未完成：直接进入教学
            if (GameManager.Instance != null && !GameManager.Instance.IsTutorialCompleted())
            {
                if (_tutorialConfig != null)
                {
                    GameManager.Instance.StartGameWithConfig(_tutorialConfig);
                }
                else
                {
                    Debug.LogError("[MainMenu] 教学模式配置未设置");
                }
                return;
            }

            // 教学已完成：打开模式选择面板
            FadeGroup(_mainMenuGroup, false, () =>
            {
                _modeSelectPanel?.Show();
            });
        }

        private void OnSettingsClicked()
        {
            // 主菜单淡出，设置面板淡入
            FadeGroup(_mainMenuGroup, false, () =>
            {
                _settingsPanel?.Show();
            });
        }

        private void OnCodexClicked()
        {
            Debug.Log("[MainMenu] 图鉴功能尚未实现");
        }

        private void OnQuitClicked()
        {
            GameManager.Instance?.QuitGame();
        }

        // ── 设置面板关闭回调（由 SettingsPanelUI 调用）──────

        public void OnSettingsClosed()
        {
            _settingsPanel?.Hide(immediate: false);
            FadeGroup(_mainMenuGroup, true);
        }

        // ── 模式选择面板关闭回调（由 ModeSelectPanelUI 调用）──

        public void OnModeSelectClosed()
        {
            _modeSelectPanel?.Hide(immediate: false);
            FadeGroup(_mainMenuGroup, true);
        }

        // ── CanvasGroup 工具 ──────────────────────────────────

        /// <summary>淡入/淡出 CanvasGroup，完成后执行 onDone</summary>
        private void FadeGroup(CanvasGroup group, bool fadeIn, System.Action onDone = null)
        {
            if (group == null)
            {
                onDone?.Invoke();
                return;
            }

            float target = fadeIn ? 1f : 0f;
            group.DOKill();
            group.DOFade(target, _fadeDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    group.interactable   = fadeIn;
                    group.blocksRaycasts = fadeIn;
                    onDone?.Invoke();
                });
        }

        /// <summary>立即设置 CanvasGroup 显示/隐藏状态</summary>
        private void ShowGroup(CanvasGroup group, bool show)
        {
            if (group == null) return;
            group.alpha          = show ? 1f : 0f;
            group.interactable   = show;
            group.blocksRaycasts = show;
        }
    }
}
