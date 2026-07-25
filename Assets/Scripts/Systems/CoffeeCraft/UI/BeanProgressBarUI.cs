using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 豆量进度条UI组件
    /// 显示当前批次的豆量和豆种
    /// </summary>
    public class BeanProgressBarUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] [Tooltip("进度条背景")]
        private Image _background;

        [SerializeField] [Tooltip("进度条填充")]
        private Image _fill;

        [SerializeField] [Tooltip("豆量文本")]
        private TextMeshProUGUI _beanAmountText;

        [Header("显隐设置")]
        [SerializeField] [Tooltip("控制整个豆量进度条显隐的 CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField] [Tooltip("渐隐渐出时长（秒）")]
        [Range(0f, 1f)]
        private float _fadeDuration = 0.2f;

        [Header("颜色")]
        [SerializeField] [Tooltip("默认颜色（未选择豆种时）")]
        private Color _defaultColor = new Color(0.5f, 0.5f, 0.5f);

        private CoffeeCraftManager _manager;
        private bool _isVisible;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;
            _canvasGroup ??= GetComponent<CanvasGroup>();

            // 初始化显示
            if (_fill != null)
            {
                _fill.fillAmount = 0f;
                _fill.color = _defaultColor;
            }

            UpdateBeanAmountText(0f, GetMaxBeanAmount());
            SetVisible(false, true);
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged += OnBatchDataChanged;
                RefreshDisplay(_manager.CurrentBatch);
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged -= OnBatchDataChanged;
            }

            _canvasGroup?.DOKill();
        }

        /// <summary>
        /// 批次数据变化回调
        /// </summary>
        private void OnBatchDataChanged(CurrentBeanBatchData batch)
        {
            RefreshDisplay(batch);
        }

        /// <summary>
        /// 刷新进度条显示
        /// </summary>
        public void RefreshDisplay(CurrentBeanBatchData batch)
        {
            if (batch == null)
            {
                UpdateFillAmount(0f);
                UpdateBeanAmountText(0f, GetMaxBeanAmount());
                UpdateFillColor(_defaultColor);
                SetVisible(false);
                return;
            }

            // 研磨完成后，取豆进度条应视为已清空
            bool hasGround = batch.grindType.HasValue;
            float displayAmount = hasGround ? 0f : batch.beanGram;

            // 更新填充量
            float maxBeanAmount = GetMaxBeanAmount();
            UpdateFillAmount(displayAmount / maxBeanAmount);

            // 更新文本
            UpdateBeanAmountText(displayAmount, maxBeanAmount);

            // 更新颜色
            if (batch.bean != null && !hasGround)
            {
                Color beanColor = GetBeanColor(batch.bean);
                UpdateFillColor(beanColor);
            }
            else
            {
                UpdateFillColor(_defaultColor);
            }

            SetVisible(displayAmount > 0f);
        }

        /// <summary>
        /// 获取当前制作规则允许的单批次最大豆量。
        /// </summary>
        private float GetMaxBeanAmount()
        {
            return GameplayBalanceManager.Instance.Config.CoffeeCraft.maxBeanPerBatch;
        }

        /// <summary>
        /// 根据当前是否已取豆，控制整个进度条渐隐渐出。
        /// </summary>
        private void SetVisible(bool visible, bool immediate = false)
        {
            if (_canvasGroup == null || (!immediate && _isVisible == visible))
            {
                return;
            }

            _isVisible = visible;
            _canvasGroup.DOKill();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            float targetAlpha = visible ? 1f : 0f;
            if (immediate || _fadeDuration <= 0f)
            {
                _canvasGroup.alpha = targetAlpha;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
                return;
            }

            _canvasGroup.DOFade(targetAlpha, _fadeDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _canvasGroup.interactable = visible;
                    _canvasGroup.blocksRaycasts = visible;
                });
        }

        /// <summary>
        /// 更新填充量
        /// </summary>
        private void UpdateFillAmount(float fillAmount)
        {
            if (_fill != null)
            {
                _fill.fillAmount = Mathf.Clamp01(fillAmount);
            }
        }

        /// <summary>
        /// 更新填充颜色
        /// </summary>
        private void UpdateFillColor(Color color)
        {
            if (_fill != null)
            {
                _fill.color = color;
            }
        }

        /// <summary>
        /// 更新豆量文本
        /// </summary>
        private void UpdateBeanAmountText(float currentAmount, float maxAmount)
        {
            if (_beanAmountText != null)
            {
                _beanAmountText.text = $"{currentAmount:F0}g / {maxAmount:F0}g";
            }
        }

        /// <summary>
        /// 根据豆种获取颜色（从BeanSO配置读取）
        /// </summary>
        private Color GetBeanColor(BeanSO bean)
        {
            if (bean == null)
            {
                return _defaultColor;
            }

            return bean.displayColor;
        }

        /// <summary>
        /// 手动刷新显示（用于测试）
        /// </summary>
        public void ManualRefresh()
        {
            if (_manager != null && _manager.CurrentBatch != null)
            {
                RefreshDisplay(_manager.CurrentBatch);
            }
        }
    }
}
