using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 动态容量条UI组件
    /// 显示咖啡液段、辅助液段和目标区
    /// </summary>
    public class VolumeBarUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] [Tooltip("容量条背景")]
        private Image _background;

        [SerializeField] [Tooltip("液体段容器")]
        private RectTransform _liquidContainer;

        [SerializeField] [Tooltip("绿色目标区")]
        private Image _targetZone;

        [SerializeField] [Tooltip("警告文本")]
        private TextMeshProUGUI _warningText;

        [Header("预制体")]
        [SerializeField] [Tooltip("液体段预制体")]
        private GameObject _liquidSegmentPrefab;

        [Header("配置")]
        [SerializeField] [Tooltip("完美误差百分比（用于计算目标区宽度）")]
        private float _perfectTolerancePercent = 0.1f;

        [SerializeField] [Tooltip("目标区颜色")]
        private Color _targetZoneColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

        private CoffeeCraftManager _manager;
        private List<Image> _liquidSegments = new List<Image>();

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            if (_targetZone != null)
            {
                _targetZone.color = _targetZoneColor;
            }

            if (_warningText != null)
            {
                _warningText.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged += OnCoffeeDataChanged;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged -= OnCoffeeDataChanged;
            }
        }

        /// <summary>
        /// 咖啡数据变化回调
        /// </summary>
        private void OnCoffeeDataChanged(CoffeeData data)
        {
            RefreshDisplay(data);
        }

        /// <summary>
        /// 刷新整体显示
        /// </summary>
        public void RefreshDisplay(CoffeeData data)
        {
            if (data == null || data.selectedCup == null)
            {
                ClearSegments();
                HideTargetZone();
                return;
            }

            RefreshLiquidSegments(data);
            RefreshTargetZone(data);
        }

        /// <summary>
        /// 刷新液体段显示
        /// </summary>
        private void RefreshLiquidSegments(CoffeeData data)
        {
            ClearSegments();

            if (data.selectedCup == null) return;

            float cupCapacity = data.selectedCup.capacity;
            float currentX = 0f;

            // 显示咖啡液段（使用BeanSO中配置的颜色）
            foreach (var coffeeSegment in data.coffeeSegments)
            {
                float widthPercent = coffeeSegment.extractedVolume / cupCapacity;
                Color coffeeColor = coffeeSegment.bean != null
                    ? coffeeSegment.bean.displayColor
                    : new Color(0.4f, 0.2f, 0.1f, 1f);
                CreateLiquidSegment(currentX, widthPercent, coffeeColor);
                currentX += widthPercent;
            }

            // 显示辅助液段
            foreach (var liquidSegment in data.liquidSegments)
            {
                float widthPercent = liquidSegment.amountMl / cupCapacity;
                Color liquidColor = GetLiquidColor(liquidSegment.liquid);
                CreateLiquidSegment(currentX, widthPercent, liquidColor);
                currentX += widthPercent;
            }
        }

        /// <summary>
        /// 创建液体段
        /// </summary>
        private void CreateLiquidSegment(float startX, float widthPercent, Color color)
        {
            GameObject segmentObj;

            if (_liquidSegmentPrefab != null)
            {
                segmentObj = Instantiate(_liquidSegmentPrefab, _liquidContainer);
            }
            else
            {
                segmentObj = new GameObject("LiquidSegment");
                segmentObj.transform.SetParent(_liquidContainer, false);
                segmentObj.AddComponent<Image>();
            }

            RectTransform rectTransform = segmentObj.GetComponent<RectTransform>();
            Image image = segmentObj.GetComponent<Image>();

            // 重置缩放
            rectTransform.localScale = Vector3.one;

            // 设置锚点和位置
            rectTransform.anchorMin = new Vector2(startX, 0f);
            rectTransform.anchorMax = new Vector2(startX + widthPercent, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // 设置颜色
            image.color = color;

            _liquidSegments.Add(image);
        }

        /// <summary>
        /// 刷新目标区显示
        /// </summary>
        private void RefreshTargetZone(CoffeeData data)
        {
            if (_manager.CurrentOrder == null || data.selectedCup == null)
            {
                HideTargetZone();
                return;
            }

            float targetVolume = _manager.CurrentOrder.targetTotalVolume;
            float cupCapacity = data.selectedCup.capacity;

            // 检查杯子容量是否足够
            if (targetVolume > cupCapacity)
            {
                HideTargetZone();
                ShowWarning("杯子容量不足！");
                return;
            }

            HideWarning();

            // 计算完美范围
            float minPerfect = targetVolume * (1f - _perfectTolerancePercent);
            float maxPerfect = targetVolume * (1f + _perfectTolerancePercent);

            minPerfect = Mathf.Clamp(minPerfect, 0f, cupCapacity);
            maxPerfect = Mathf.Clamp(maxPerfect, 0f, cupCapacity);

            float startPercent = minPerfect / cupCapacity;
            float endPercent = maxPerfect / cupCapacity;

            ShowTargetZone(startPercent, endPercent);
        }

        /// <summary>
        /// 显示目标区
        /// </summary>
        private void ShowTargetZone(float startPercent, float endPercent)
        {
            if (_targetZone == null) return;

            _targetZone.gameObject.SetActive(true);

            RectTransform rectTransform = _targetZone.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(startPercent, 0f);
            rectTransform.anchorMax = new Vector2(endPercent, 1f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 隐藏目标区
        /// </summary>
        private void HideTargetZone()
        {
            if (_targetZone != null)
            {
                _targetZone.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 显示警告
        /// </summary>
        private void ShowWarning(string message)
        {
            if (_warningText != null)
            {
                _warningText.text = message;
                _warningText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 隐藏警告
        /// </summary>
        private void HideWarning()
        {
            if (_warningText != null)
            {
                _warningText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 清空所有液体段
        /// </summary>
        private void ClearSegments()
        {
            foreach (var segment in _liquidSegments)
            {
                if (segment != null)
                {
                    Destroy(segment.gameObject);
                }
            }
            _liquidSegments.Clear();
        }

        /// <summary>
        /// 根据液体配置获取颜色
        /// </summary>
        private Color GetLiquidColor(LiquidSO liquid)
        {
            if (liquid == null)
            {
                return Color.white;
            }

            // 使用 LiquidSO 的 displayColor 属性
            return liquid.displayColor;
        }
    }
}
