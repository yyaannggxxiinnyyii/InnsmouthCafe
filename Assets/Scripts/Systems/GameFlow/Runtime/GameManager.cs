using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏全局管理器
/// 负责分辨率/窗口设置持久化与场景切换等全局功能
/// 跨场景持久存在
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("场景名称")]
    [SerializeField]
    [Tooltip("主菜单场景名")]
    private string _mainMenuSceneName = "MainScene";

    // PlayerPrefs 键名
    private const string PrefKeyDisplayMode = "DisplayMode";      // 0=窗口 1=全屏
    private const string PrefKeyResolutionIndex = "ResolutionIndex";
    private const string SaveExistsKey = "SaveExists";

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

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        LoadAndApplyDisplaySettings();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载完成回调：主菜单和游戏场景统一切回默认主轨道BGM。
    /// 结局BGM由 EndingPanelUI 在触发结局时自行切换，无需在此处理。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == _mainMenuSceneName)
        {
            AudioManager.Instance?.PlayDefaultBgm();
        }
    }

    // ── 显示设置 ──────────────────────────────────────────

    /// <summary>
    /// 应用并持久化显示设置
    /// </summary>
    /// <param name="fullscreen">是否全屏</param>
    /// <param name="resolutionIndex">窗口模式下的分辨率索引</param>
    public void ApplyDisplaySettings(bool fullscreen, int resolutionIndex)
    {
        IsFullscreen = fullscreen;
        ResolutionIndex = Mathf.Clamp(resolutionIndex, 0, WindowedResolutions.Length - 1);

        PlayerPrefs.SetInt(PrefKeyDisplayMode, fullscreen ? 1 : 0);
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
        bool fullscreen = PlayerPrefs.GetInt(PrefKeyDisplayMode, 0) == 1;
        int resIndex = PlayerPrefs.GetInt(PrefKeyResolutionIndex, 2); // 默认 1920x1080
        ApplyDisplaySettings(fullscreen, resIndex);
    }

    // ── 场景切换 ──────────────────────────────────────────

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

    /// <summary>重置游戏进度（清除存档标记与收集物状态）</summary>
    public void ResetGameProgress()
    {
        PlayerPrefs.DeleteKey(SaveExistsKey);

        // 重置收集物
        if (CollectibleManager.Instance != null)
            CollectibleManager.Instance.ResetAllCollectibles();
        else
            PlayerPrefs.SetInt("Collectible_ResetFlag", 1);

        GalleryManager.Instance?.ResetAllGalleryData();

        PlayerPrefs.Save();
        Debug.Log("[GameManager] 游戏进度已重置");
    }

#if UNITY_EDITOR
    /// <summary>编辑器调试：重置游戏进度</summary>
    [ContextMenu("调试：重置游戏进度")]
    private void DebugResetGameProgress()
    {
        ResetGameProgress();
    }
#endif
}
