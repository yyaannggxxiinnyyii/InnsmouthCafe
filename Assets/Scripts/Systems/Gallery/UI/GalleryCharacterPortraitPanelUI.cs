using InnsmouthCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 角色图鉴立绘详情面板UI，负责显示角色四种立绘，并在表情立绘未完整记录时显示占位立绘。
    /// </summary>
    public class GalleryCharacterPortraitPanelUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField]
        [Tooltip("角色立绘详情面板 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        [Tooltip("关闭角色立绘详情面板按钮")]
        private Button _closeButton;

        [Header("四种立绘")]
        [SerializeField]
        [Tooltip("等待或正常立绘")]
        private Image _normalPortrait;

        [SerializeField]
        [Tooltip("不耐烦立绘")]
        private Image _impatientPortrait;

        [SerializeField]
        [Tooltip("愤怒立绘")]
        private Image _angryPortrait;

        [SerializeField]
        [Tooltip("开心立绘")]
        private Image _happyPortrait;

        [Header("锁定显示")]
        [SerializeField]
        [Tooltip("四张立绘未完整解锁时显示的占位立绘")]
        private Sprite _lockedPortraitSprite;

        private void Awake()
        {
            _closeButton?.onClick.AddListener(Hide);
            Hide();
        }

        /// <summary>
        /// 显示角色立绘详情；未完整记录时显示常态立绘，表情位置展示占位立绘，完整记录后展示四种立绘。
        /// </summary>
        public void Show(CustomerSO customer, bool isPerfected)
        {
            if (customer == null)
            {
                Hide();
                return;
            }

            if (!isPerfected)
            {
                SetSprite(_normalPortrait, customer.normalSprite);
                SetSprite(_impatientPortrait, _lockedPortraitSprite);
                SetSprite(_angryPortrait, _lockedPortraitSprite);
                SetSprite(_happyPortrait, _lockedPortraitSprite);
                SetCanvasGroupVisible(_canvasGroup, true);
                return;
            }

            SetSprite(_normalPortrait, customer.normalSprite);
            SetSprite(
                _impatientPortrait,
                GetFallbackSprite(customer.impatientSprite, customer.normalSprite));
            SetSprite(
                _angryPortrait,
                GetFallbackSprite(customer.angrySprite, customer.normalSprite));
            SetSprite(
                _happyPortrait,
                GetFallbackSprite(customer.happySprite, customer.normalSprite));
            SetCanvasGroupVisible(_canvasGroup, true);
        }

        /// <summary>
        /// 隐藏角色立绘详情面板。
        /// </summary>
        public void Hide()
        {
            SetCanvasGroupVisible(_canvasGroup, false);
        }

        /// <summary>
        /// 获取表情立绘；当表情图为空时回退到默认立绘。
        /// </summary>
        private Sprite GetFallbackSprite(Sprite sprite, Sprite fallbackSprite)
        {
            return sprite != null ? sprite : fallbackSprite;
        }

        /// <summary>
        /// 设置图片内容；保持 Image 激活，避免锁定态布局变化。
        /// </summary>
        private void SetSprite(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
        }

        /// <summary>
        /// 通过 CanvasGroup 设置面板显隐与交互。
        /// </summary>
        private void SetCanvasGroupVisible(CanvasGroup canvasGroup, bool visible)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
