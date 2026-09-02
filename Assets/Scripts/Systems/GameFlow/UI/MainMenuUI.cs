using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Managers;
using InnsmouthCafe.Persistence;
using UnityEngine.SceneManagement;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 主菜单UI管理器
    /// 使用 CanvasGroup + DOTween 在主菜单页和设置页之间淡入淡出切换
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        private const string LongTermGameSceneName = "GameScene_DioramaPrototype";

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

        [Header("图鉴面板")]
        [SerializeField] [Tooltip("主菜单中的图鉴面板UI")]
        private GalleryPanelUI _galleryPanel;

        [Header("按钮文本")]
        [SerializeField] [Tooltip("开始按钮的文本组件")]
        private TextMeshProUGUI _startButtonText;

        [SerializeField] [Tooltip("有存档时开始按钮显示的文本")]
        private string _newGameText = "新建存档";

        [SerializeField] [Tooltip("无存档时开始按钮显示的文本")]
        private string _startGameText = "开始游戏";

        [Header("设置面板")]
        [SerializeField] [Tooltip("设置面板 UI 脚本（用于打开/关闭回调）")]
        private SettingsPanelUI _settingsPanel;

        [Header("存档选择面板")]
        [SerializeField] [Tooltip("三个固定存档槽位的选择面板")]
        private SaveSlotPanelUI _saveSlotPanel;

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
            if (_saveSlotPanel != null)
                _saveSlotPanel.Hide(immediate: true);
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
            bool hasSave = SaveSlotService.Instance.HasAnySave();

            if (_continueButton != null)
                _continueButton.gameObject.SetActive(hasSave);

            if (_startButtonText != null)
                _startButtonText.text = hasSave ? _newGameText : _startGameText;
        }

        // ── 按钮回调 ──────────────────────────────────────────

        private void OnContinueClicked()
        {
            if (!SaveSlotService.Instance.TryLoadMostRecentSave(out GameSaveData saveData))
            {
                RefreshButtonStates();
                return;
            }

            OnSaveSelected(saveData);
        }

        private void OnStartClicked()
        {
            OpenSaveSlotPanel();
        }

        private void OpenSaveSlotPanel()
        {
            if (_saveSlotPanel == null)
            {
                Debug.LogError("[MainMenu] 存档选择面板未配置");
                return;
            }

            RefreshButtonStates();
            FadeGroup(_mainMenuGroup, false, () => _saveSlotPanel.Show());
        }

        /// <summary>
        /// 接收存档面板选中的存档，并进入对应的游戏场景。
        /// </summary>
        /// <param name="saveData">已创建或已加载的当前存档</param>
        public void OnSaveSelected(GameSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            SceneManager.LoadScene(LongTermGameSceneName);
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
            if (_galleryPanel == null)
            {
                _galleryPanel = GalleryPanelSceneKeeper.GetGalleryPanel();
            }

            if (_galleryPanel == null)
            {
                _galleryPanel = FindObjectOfType<GalleryPanelUI>(true);
            }

            if (_galleryPanel == null)
            {
                Debug.LogWarning("[MainMenu] 图鉴面板未配置，无法打开图鉴");
                return;
            }

            _galleryPanel.SetEndingReplayEnabled(true);
            _galleryPanel.Show();
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

        // ── 存档选择面板关闭回调（由 SaveSlotPanelUI 调用）──

        /// <summary>
        /// 关闭存档选择面板并返回主菜单。
        /// </summary>
        public void OnSaveSlotClosed()
        {
            _saveSlotPanel?.Hide(immediate: false);
            FadeGroup(_mainMenuGroup, true);
        }

        [System.Obsolete("旧模式选择面板已移除；场景改造完成后删除该回调。")]
        public void OnModeSelectClosed()
        {
            OnSaveSlotClosed();
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
