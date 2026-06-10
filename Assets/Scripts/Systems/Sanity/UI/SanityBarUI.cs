using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 理智值条UI
    /// 订阅 SanityManager.OnSanityChanged，同步更新 Image.fillAmount
    /// 鼠标悬停时在 _valueText 上显示当前理智值，颜色随数值由绿渐变为红
    /// </summary>
    public class SanityBarUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("组件引用")]
        [SerializeField] [Tooltip("填充图片（Image Type = Filled）")]
        private Image _fillImage;

        [SerializeField] [Tooltip("显示理智数值的 TextMeshPro（鼠标悬停时可见）")]
        private TextMeshProUGUI _valueText;

        [Header("动画设置")]
        [SerializeField] [Tooltip("填充变化的补间时长（秒），0则立即更新")]
        private float _tweenDuration = 0.4f;

        private void Start()
        {
            if (_valueText != null)
                _valueText.gameObject.SetActive(false);

            if (SanityManager.Instance != null)
            {
                _fillImage.fillAmount = SanityManager.Instance.SanityRatio;
                SanityManager.Instance.OnSanityChanged += OnSanityChanged;
            }
        }

        private void OnDestroy()
        {
            if (SanityManager.Instance != null)
                SanityManager.Instance.OnSanityChanged -= OnSanityChanged;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_valueText == null) return;
            RefreshValueText();
            _valueText.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_valueText == null) return;
            _valueText.gameObject.SetActive(false);
        }

        private void OnSanityChanged(float oldValue, float newValue, string reason)
        {
            float targetRatio = SanityManager.Instance.SanityRatio;

            if (_tweenDuration > 0f)
            {
                _fillImage.DOKill();
                _fillImage.DOFillAmount(targetRatio, _tweenDuration).SetEase(Ease.OutQuad);
            }
            else
            {
                _fillImage.fillAmount = targetRatio;
            }

            if (_valueText != null && _valueText.gameObject.activeSelf)
                RefreshValueText();
        }

        private void RefreshValueText()
        {
            if (SanityManager.Instance == null) return;

            float ratio = SanityManager.Instance.SanityRatio;
            _valueText.color = Color.Lerp(Color.red, Color.green, ratio);
            _valueText.text = SanityManager.Instance.CurrentSanity.ToString("F1");
        }
    }
}
