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
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OpenGallery);
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
    }
}
