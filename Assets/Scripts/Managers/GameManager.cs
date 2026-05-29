using UnityEngine;
using UnityEngine.SceneManagement;
using InnsmouthCafe.Data;

namespace InnsmouthCafe.Managers
{
    /// <summary>
    /// 游戏全局管理器
    /// 负责分辨率/窗口设置持久化、场景切换、模式解锁管理等全局功能
    /// 跨场景持久存在
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("场景名称")]
        [SerializeField] [Tooltip("主菜单场景名")]
        private string _mainMenuSceneName = "MainScene";

        [SerializeField] [Tooltip("游戏场景名")]
        private string _gameSceneName = "GameScene";

        // PlayerPrefs 键名
        private const string PrefKeyDisplayMode      = "DisplayMode";      // 0=窗口 1=全屏
        private const string PrefKeyResolutionIndex  = "ResolutionIndex";
        private const string SaveExistsKey           = "SaveExists";

        // 模式解锁 PlayerPrefs 键名
        private const string PrefKeyTutorialDone     = "ModeUnlock_Tutorial_Done";
        private const string PrefKeyBeginnerUnlocked = "ModeUnlock_Beginner";
        private const string PrefKeyNormalUnlocked   = "ModeUnlock_Normal";

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

        /// <summary>当前选中的游戏模式配置（场景切换时传递）</summary>
        public GameModeConfigSO SelectedModeConfig { get; private set; }

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

        /// <summary>以指定模式配置启动游戏场景</summary>
        public void StartGameWithConfig(GameModeConfigSO config)
        {
            if (config == null)
            {
                Debug.LogError("[GameManager] GameModeConfigSO 为空，无法启动游戏");
                return;
            }

            SelectedModeConfig = config;
            PlayerPrefs.DeleteKey(SaveExistsKey);
            PlayerPrefs.Save();
            SceneManager.LoadScene(_gameSceneName);
        }

        /// <summary>开始新游戏（兼容旧调用，不推荐直接使用）</summary>
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

        // ── 模式解锁管理 ──────────────────────────────────────

        /// <summary>教学模式是否已完成</summary>
        public bool IsTutorialCompleted()
        {
            return PlayerPrefs.GetInt(PrefKeyTutorialDone, 0) == 1;
        }

        /// <summary>标记教学模式完成，同时解锁新手模式</summary>
        public void MarkTutorialCompleted()
        {
            PlayerPrefs.SetInt(PrefKeyTutorialDone, 1);
            PlayerPrefs.SetInt(PrefKeyBeginnerUnlocked, 1);
            PlayerPrefs.Save();
            Debug.Log("[GameManager] 教学模式完成，新手模式已解锁");
        }

        /// <summary>查询指定模式是否已解锁</summary>
        public bool IsModeUnlocked(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Tutorial:
                    return true; // 教学模式始终可用
                case GameMode.Beginner:
                    return PlayerPrefs.GetInt(PrefKeyBeginnerUnlocked, 0) == 1;
                case GameMode.Normal:
                    return PlayerPrefs.GetInt(PrefKeyNormalUnlocked, 0) == 1;
                case GameMode.Endless:
                    return false; // 暂不实现
                default:
                    return false;
            }
        }

        /// <summary>解锁指定模式</summary>
        public void UnlockMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Beginner:
                    PlayerPrefs.SetInt(PrefKeyBeginnerUnlocked, 1);
                    break;
                case GameMode.Normal:
                    PlayerPrefs.SetInt(PrefKeyNormalUnlocked, 1);
                    break;
                default:
                    Debug.LogWarning($"[GameManager] 不支持解锁模式: {mode}");
                    return;
            }
            PlayerPrefs.Save();
            Debug.Log($"[GameManager] 模式已解锁: {mode}");
        }

        /// <summary>重置游戏进度（清除存档标记和所有模式解锁状态）</summary>
        public void ResetGameProgress()
        {
            PlayerPrefs.DeleteKey(SaveExistsKey);
            PlayerPrefs.DeleteKey(PrefKeyTutorialDone);
            PlayerPrefs.DeleteKey(PrefKeyBeginnerUnlocked);
            PlayerPrefs.DeleteKey(PrefKeyNormalUnlocked);
            PlayerPrefs.Save();
            Debug.Log("[GameManager] 游戏进度已重置");
        }

#if UNITY_EDITOR
        /// <summary>编辑器调试：重置所有模式解锁状态</summary>
        [ContextMenu("调试：重置模式解锁")]
        private void DebugResetModeUnlock()
        {
            ResetGameProgress();
        }

        /// <summary>编辑器调试：解锁所有模式</summary>
        [ContextMenu("调试：解锁所有模式")]
        private void DebugUnlockAllModes()
        {
            PlayerPrefs.SetInt(PrefKeyTutorialDone, 1);
            PlayerPrefs.SetInt(PrefKeyBeginnerUnlocked, 1);
            PlayerPrefs.SetInt(PrefKeyNormalUnlocked, 1);
            PlayerPrefs.Save();
            Debug.Log("[GameManager] 所有模式已解锁");
        }
#endif
    }
}
