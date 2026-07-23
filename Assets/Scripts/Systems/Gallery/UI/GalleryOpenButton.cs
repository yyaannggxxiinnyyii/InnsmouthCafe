using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 图鉴入口按钮，运行时查找跨场景保留的图鉴面板并打开。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GalleryOpenButton : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("图鉴入口红点")]
        private GameObject _redDot;

        private Button _button;
        private GalleryManager _galleryManager;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OpenGallery);
        }

        private void OnEnable()
        {
            BindGalleryManager();
            RefreshRedDot();
        }

        private void OnDisable()
        {
            UnbindGalleryManager();
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OpenGallery);
            }
        }

        /// <summary>
        /// 打开跨场景保留的图鉴面板；未找到时输出警告。
        /// </summary>
        private void OpenGallery()
        {
            GalleryPanelUI galleryPanel = GalleryPanelSceneKeeper.GetGalleryPanel();
            if (galleryPanel == null)
            {
                galleryPanel = FindObjectOfType<GalleryPanelUI>(true);
            }

            if (galleryPanel == null)
            {
                Debug.LogWarning("[Gallery] 未找到图鉴面板，无法通过入口按钮打开图鉴");
                return;
            }

            galleryPanel.Show();
        }

        /// <summary>
        /// 绑定图鉴状态变化事件，用于实时刷新入口红点。
        /// </summary>
        private void BindGalleryManager()
        {
            GalleryManager galleryManager = GalleryManager.Instance;
            if (_galleryManager == galleryManager)
            {
                return;
            }

            UnbindGalleryManager();
            _galleryManager = galleryManager;
            if (_galleryManager != null)
            {
                _galleryManager.OnGalleryChanged += RefreshRedDot;
            }
        }

        /// <summary>
        /// 解绑图鉴状态变化事件。
        /// </summary>
        private void UnbindGalleryManager()
        {
            if (_galleryManager != null)
            {
                _galleryManager.OnGalleryChanged -= RefreshRedDot;
            }

            _galleryManager = null;
        }

        /// <summary>
        /// 根据图鉴未读状态刷新入口红点。
        /// </summary>
        private void RefreshRedDot()
        {
            if (_redDot != null)
            {
                _redDot.SetActive(_galleryManager != null && _galleryManager.HasAnyUnread());
            }
        }
    }
}
