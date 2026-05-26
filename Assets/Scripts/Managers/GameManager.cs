using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 游戏全局管理器
    /// 负责分辨率/窗口设置持久化、场景切换等全局功能
    /// 跨场景持久存在
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("场景名称")]
        [SerializeField] [Tooltip("主菜单场景名")]
        private string _mainMenuSceneName = "MainMenu";

        [SerializeField] [Tooltip("游戏场景名")]
        private string _gameSceneName = "GameScene";

        // PlayerPrefs 键名
        private const string PrefKeyDisplayMode      = "DisplayMode";      // 0=窗口 1=全屏
        private const string PrefKeyResolutionIndex  = "ResolutionIndex";
        private const string SaveExistsKey           = "SaveExists";

        /// <summary>窗口模式下可选分辨率列表</summary>
        public static readonly (int width, int height)[] WindowedResolutions =
        {
            (1280,  720),
            (1600,  900),
            (1920, 1080),
            (2560, 1440),
        };

        /// <summary>当前是否全屏</summary>
        public bool IsFullscreen { get; private set; }

        /// <summary>当前窗口分辨率索引</summary>
        public int ResolutionIndex { get; private set; }

        /// <summary>是否有存档</summary>
        public bool HasSaveData => PlayerPrefs.HasKey(SaveExistsKey);

        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
            LoadAndApplyDisplaySettings();
        }

        // ── 显示设置 ──────────────────────────────────────────

        /// <summary>
        /// 应用并持久化显示设置
        /// </summary>
        /// <param name="fullscreen">是否全屏</param>
        /// <param name="resolutionIndex">窗口模式下的分辨率索引</param>
        public void ApplyDisplaySettings(bool fullscreen, int resolutionIndex)
        {
            IsFullscreen     = fullscreen;
            ResolutionIndex  = Mathf.Clamp(resolutionIndex, 0, WindowedResolutions.Length - 1);

            PlayerPrefs.SetInt(PrefKeyDisplayMode,     fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(PrefKeyResolutionIndex, ResolutionIndex);
            PlayerPrefs.Save();

            if (fullscreen)
            {
                Screen.SetResolution(
                    Display.main.systemWidth,
                    Display.main.systemHeight,
                    FullScreenMode.FullScreenWindow
                );
            }
            else
            {
                var res = WindowedResolutions[ResolutionIndex];
                Screen.SetResolution(res.width, res.height, FullScreenMode.Windowed);
            }
        }

        /// <summary>从 PlayerPrefs 加载并立即应用显示设置</summary>
        private void LoadAndApplyDisplaySettings()
        {
            bool fullscreen     = PlayerPrefs.GetInt(PrefKeyDisplayMode,     0) == 1;
            int  resIndex       = PlayerPrefs.GetInt(PrefKeyResolutionIndex, 2); // 默认 1920x1080
            ApplyDisplaySettings(fullscreen, resIndex);
        }

        // ── 场景切换 ──────────────────────────────────────────

        /// <summary>开始新游戏</summary>
        public void StartNewGame()
        {
            PlayerPrefs.DeleteKey(SaveExistsKey);
            PlayerPrefs.Save();
            SceneManager.LoadScene(_gameSceneName);
        }

        /// <summary>继续游戏</summary>
        public void ContinueGame()
        {
            if (!HasSaveData)
            {
                Debug.LogWarning("[GameManager] 没有存档，无法继续游戏");
                return;
            }
            SceneManager.LoadScene(_gameSceneName);
        }

        /// <summary>返回主菜单</summary>
        public void GoToMainMenu()
        {
            SceneManager.LoadScene(_mainMenuSceneName);
        }

        /// <summary>退出游戏</summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>创建存档标记</summary>
        public void MarkSaveExists()
        {
            PlayerPrefs.SetInt(SaveExistsKey, 1);
            PlayerPrefs.Save();
        }
    }
}
