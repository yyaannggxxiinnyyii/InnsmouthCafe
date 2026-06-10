using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 暂停面板UI
    /// 控制游戏暂停/恢复、重新开始、返回主菜单、打开设置
    /// </summary>
    public class PausePanelUI : MonoBehaviour
    {
        [Header("CanvasGroup")]
        [SerializeField] [Tooltip("暂停面板自身的 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [Header("按钮")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("设置子面板")]
        [SerializeField] [Tooltip("复用的 SettingsPanelUI（与主界面同一个 Prefab 或独立实例均可）")]
        private SettingsPanelUI _settingsPanel;

        [Header("暂停按钮")]
        [SerializeField] [Tooltip("游戏界面上的暂停按钮，点击打开此面板")]
        private Button _pauseButton;

        [Header("过渡设置")]
        [SerializeField] private float _fadeDuration = 0.2f;

        private bool _isPaused;

        private void Awake()
        {
            _resumeButton?.onClick.AddListener(Resume);
            _restartButton?.onClick.AddListener(OnRestartClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
            _pauseButton?.onClick.AddListener(Pause);

            // 设置面板关闭时回到暂停面板
            if (_settingsPanel != null)
                _settingsPanel.OnCloseCallback = OnSettingsClosed;

            // 初始隐藏
            SetGroupState(false);
            _settingsPanel?.Hide(immediate: true);
        }

        // ── 暂停 / 恢复 ───────────────────────────────────────

        /// <summary>暂停游戏并显示面板</summary>
        public void Pause()
        {
            if (_isPaused) return;
            _isPaused = true;
            Time.timeScale = 0f;
            FadePanel(true);
        }

        /// <summary>恢复游戏并隐藏面板</summary>
        public void Resume()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            FadePanel(false);
        }

        // ── 按钮回调 ──────────────────────────────────────────

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            _isPaused = false;

            if (GameManager.Instance != null && GameManager.Instance.SelectedModeConfig != null)
                GameManager.Instance.StartGameWithConfig(GameManager.Instance.SelectedModeConfig);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnSettingsClicked()
        {
            _settingsPanel?.Show();
        }

        private void OnSettingsClosed()
        {
            _settingsPanel?.Hide(immediate: true);
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            _isPaused = false;

            if (GameManager.Instance != null)
                GameManager.Instance.GoToMainMenu();
            else
                SceneManager.LoadScene("MainScene");
        }

        // ── ESC 键支持 ────────────────────────────────────────

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isPaused) Resume();
                else Pause();
            }
        }

        // ── CanvasGroup 工具 ──────────────────────────────────

        private void FadePanel(bool fadeIn)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.DOKill();

            if (fadeIn)
            {
                _canvasGroup.alpha          = 0f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable   = false;
                _canvasGroup.DOFade(1f, _fadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(() => _canvasGroup.interactable = true);
            }
            else
            {
                _canvasGroup.interactable = false;
                _canvasGroup.DOFade(0f, _fadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(() => _canvasGroup.blocksRaycasts = false);
            }
        }

        private void SetGroupState(bool visible)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha          = visible ? 1f : 0f;
            _canvasGroup.interactable   = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
    }
}
