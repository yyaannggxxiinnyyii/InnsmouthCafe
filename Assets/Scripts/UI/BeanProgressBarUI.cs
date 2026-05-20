using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        [Header("配置")]
        [SerializeField] [Tooltip("最大豆量（克）")]
        private float _maxBeanAmount = 20f;

        [SerializeField] [Tooltip("每格豆量（克）")]
        private float _beanPerGrid = 5f;

        [Header("颜色")]
        [SerializeField] [Tooltip("默认颜色（未选择豆种时）")]
        private Color _defaultColor = new Color(0.5f, 0.5f, 0.5f);

        private CoffeeCraftManager _manager;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            // 初始化显示
            if (_fill != null)
            {
                _fill.fillAmount = 0f;
                _fill.color = _defaultColor;
            }

            UpdateBeanAmountText(0f, _maxBeanAmount);
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged += OnBatchDataChanged;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnBatchDataChanged -= OnBatchDataChanged;
            }
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
                UpdateBeanAmountText(0f, _maxBeanAmount);
                UpdateFillColor(_defaultColor);
                return;
            }

            // 研磨完成后，取豆进度条应视为已清空
            bool hasGround = batch.grindType.HasValue;
            float displayAmount = hasGround ? 0f : batch.beanGram;

            // 更新填充量
            UpdateFillAmount(displayAmount / _maxBeanAmount);

            // 更新文本
            UpdateBeanAmountText(displayAmount, _maxBeanAmount);

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
