using UnityEngine;
using UnityEngine.SceneManagement;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 图鉴面板跨场景保留组件，负责保留唯一图鉴实例并按当前场景切换局外/局内行为。
    /// </summary>
    public class GalleryPanelSceneKeeper : MonoBehaviour
    {
        [Header("图鉴引用")]
        [SerializeField]
        [Tooltip("需要跨场景保留的图鉴面板UI；为空时自动在子节点查找")]
        private GalleryPanelUI _galleryPanel;

        [Header("场景设置")]
        [SerializeField]
        [Tooltip("主菜单场景名称；在该场景中允许结局图鉴重播")]
        private string _mainMenuSceneName = "MainScene";

        [SerializeField]
        [Tooltip("切换场景时是否自动隐藏图鉴面板")]
        private bool _hideOnSceneChanged = true;

        private static GalleryPanelSceneKeeper _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            CacheGalleryPanel();
            DontDestroyOnLoad(gameObject);
            ApplySceneMode(SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// 获取当前保留的图鉴面板实例。
        /// </summary>
        public static GalleryPanelUI GetGalleryPanel()
        {
            return _instance != null ? _instance._galleryPanel : null;
        }

        /// <summary>
        /// 处理活动场景变化，并刷新图鉴局内/局外行为。
        /// </summary>
        private void HandleActiveSceneChanged(Scene previousScene, Scene currentScene)
        {
            ApplySceneMode(currentScene);
        }

        /// <summary>
        /// 根据当前场景设置结局图鉴是否允许重播。
        /// </summary>
        private void ApplySceneMode(Scene scene)
        {
            CacheGalleryPanel();

            if (_galleryPanel == null)
            {
                Debug.LogWarning("[Gallery] 图鉴面板跨场景保留组件未找到 GalleryPanelUI");
                return;
            }

            bool isMainMenu = scene.name == _mainMenuSceneName;
            _galleryPanel.SetEndingReplayEnabled(isMainMenu);

            if (_hideOnSceneChanged)
            {
                _galleryPanel.Hide();
            }
        }

        /// <summary>
        /// 缓存图鉴面板引用。
        /// </summary>
        private void CacheGalleryPanel()
        {
            if (_galleryPanel == null)
            {
                _galleryPanel = GetComponentInChildren<GalleryPanelUI>(true);
            }
        }
    }
}
