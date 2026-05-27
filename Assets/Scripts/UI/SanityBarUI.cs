using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 理智值条UI
    /// 订阅 SanityManager.OnSanityChanged，同步更新 Image.fillAmount
    /// </summary>
    public class SanityBarUI : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] [Tooltip("填充图片（Image Type = Filled）")]
        private Image _fillImage;

        [Header("动画设置")]
        [SerializeField] [Tooltip("填充变化的补间时长（秒），0则立即更新")]
        private float _tweenDuration = 0.4f;

        private void Start()
        {
            if (Managers.SanityManager.Instance != null)
            {
                // 初始化为当前值
                _fillImage.fillAmount = Managers.SanityManager.Instance.SanityRatio;

                // 订阅变化事件
                Managers.SanityManager.Instance.OnSanityChanged += OnSanityChanged;
            }
        }

        private void OnDestroy()
        {
            if (Managers.SanityManager.Instance != null)
                Managers.SanityManager.Instance.OnSanityChanged -= OnSanityChanged;
        }

        private void OnSanityChanged(float oldValue, float newValue, string reason)
        {
            float targetRatio = Managers.SanityManager.Instance.SanityRatio;

            if (_tweenDuration > 0f)
            {
                _fillImage.DOKill();
                _fillImage.DOFillAmount(targetRatio, _tweenDuration).SetEase(Ease.OutQuad);
            }
            else
            {
                _fillImage.fillAmount = targetRatio;
            }
        }
    }
}
