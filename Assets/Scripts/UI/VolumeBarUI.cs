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
        [SerializeField]
        [Tooltip("容量条背景")]
        private Image _background;

        [SerializeField]
        [Tooltip("液体段容器")]
        private RectTransform _liquidContainer;

        [SerializeField]
        [Tooltip("绿色目标区")]
        private Image _targetZone;

        [SerializeField]
        [Tooltip("警告文本")]
        private TextMeshProUGUI _warningText;

        [SerializeField]
        [Tooltip("当前倒液量文本（实时显示正在倒入的辅助液累计量）")]
        private TextMeshProUGUI _currentLiquidVolumeText;

        [Header("预制体")]
        [SerializeField]
        [Tooltip("液体段预制体")]
        private GameObject _liquidSegmentPrefab;

        [Header("配置")]
        [SerializeField]
        [Tooltip("完美误差百分比（用于计算目标区宽度）")]
        private float _perfectTolerancePercent = 0.1f;

        [SerializeField]
        [Tooltip("目标区颜色")]
        private Color _targetZoneColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);

        private CoffeeCraftManager _manager;
        private List<Image> _liquidSegments = new List<Image>();

        // 辅助液段的 tooltip trigger，key = LiquidSO，用于实时更新容量描述
        private Dictionary<LiquidSO, HoverTooltipTrigger> _liquidTooltipTriggers = new Dictionary<LiquidSO, HoverTooltipTrigger>();

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

            if (_currentLiquidVolumeText != null)
            {
                _currentLiquidVolumeText.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            gameObject.SetActive(false);

            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged += OnCoffeeDataChanged;
                _manager.OnCraftReset += OnCraftReset;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged -= OnCoffeeDataChanged;
                _manager.OnCraftReset -= OnCraftReset;
            }
        }

        private void OnCraftReset()
        {
            gameObject.SetActive(false);
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
                gameObject.SetActive(false);
                ClearSegments();
                HideTargetZone();
                return;
            }

            gameObject.SetActive(true);
            RefreshLiquidSegments(data);
            RefreshTargetZone(data);
            RefreshCurrentLiquidVolumeText(data);
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

            // 显示咖啡液段（无图标无 tooltip）
            foreach (var coffeeSegment in data.coffeeSegments)
            {
                float widthPercent = coffeeSegment.extractedVolume / cupCapacity;
                Color coffeeColor = coffeeSegment.bean != null
                    ? coffeeSegment.bean.displayColor
                    : new Color(0.4f, 0.2f, 0.1f, 1f);
                CreateSegment(currentX, widthPercent, coffeeColor, null, null, 0f);
                currentX += widthPercent;
            }

            // 显示辅助液段（带图标和 tooltip）
            foreach (var liquidSegment in data.liquidSegments)
            {
                float widthPercent = liquidSegment.amountMl / cupCapacity;
                Color liquidColor = GetLiquidColor(liquidSegment.liquid);
                var trigger = CreateSegment(currentX, widthPercent, liquidColor,
                    liquidSegment.liquid?.icon, liquidSegment.liquid?.liquidName, liquidSegment.amountMl);
                if (trigger != null && liquidSegment.liquid != null)
                    _liquidTooltipTriggers[liquidSegment.liquid] = trigger;
                currentX += widthPercent;
            }
        }

        /// <summary>
        /// 创建液体段，icon 不为 null 时在段内添加图标和 Tooltip
        /// 返回挂在图标上的 HoverTooltipTrigger（无图标时返回 null）
        /// </summary>
        private HoverTooltipTrigger CreateSegment(float startX, float widthPercent, Color color,
            Sprite icon, string liquidName, float amountMl)
        {
            if (_liquidContainer == null) return null;

            GameObject segmentObj;
            if (_liquidSegmentPrefab != null)
                segmentObj = Instantiate(_liquidSegmentPrefab, _liquidContainer);
            else
            {
                segmentObj = new GameObject("LiquidSegment");
                segmentObj.transform.SetParent(_liquidContainer, false);
            }

            RectTransform rectTransform = segmentObj.GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = segmentObj.AddComponent<RectTransform>();

            Image image = segmentObj.GetComponent<Image>();
            if (image == null)
                image = segmentObj.AddComponent<Image>();

            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin  = new Vector2(startX, 0f);
            rectTransform.anchorMax  = new Vector2(startX + widthPercent, 1f);
            rectTransform.offsetMin  = Vector2.zero;
            rectTransform.offsetMax  = Vector2.zero;
            image.color = color;

            _liquidSegments.Add(image);

            // 无图标则不添加图标子节点
            if (icon == null) return null;

            // 创建图标子节点，居中填满段高度，宽高相等
            GameObject iconObj = new GameObject("LiquidIcon");
            iconObj.transform.SetParent(segmentObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin        = new Vector2(0.5f, 0f);
            iconRect.anchorMax        = new Vector2(0.5f, 1f);
            iconRect.pivot            = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta        = new Vector2(0f, 0f); // 高度跟随父节点，宽度由 SetSizeWithCurrentAnchors 设置

            // 让图标宽度等于父节点高度（正方形）
            iconRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                rectTransform.rect.height > 0 ? rectTransform.rect.height : 32f);

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite             = icon;
            iconImage.preserveAspect     = true;
            iconImage.raycastTarget      = true;

            // 挂 Tooltip trigger
            HoverTooltipTrigger trigger = iconObj.AddComponent<HoverTooltipTrigger>();
            trigger.ConfigureText(
                liquidName ?? string.Empty,
                $"{Mathf.FloorToInt(amountMl)}ml"
            );

            return trigger;
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
                ShowWarning("当前杯子容量无法满足订单！");
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
            _liquidTooltipTriggers.Clear();
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

        /// <summary>
        /// 刷新当前倒液量文本
        /// 显示正在倒入的辅助液在 liquidSegments 中的累计量（整数 ml）
        /// 没有正在倒液时隐藏
        /// </summary>
        private void RefreshCurrentLiquidVolumeText(CoffeeData data)
        {
            if (_currentLiquidVolumeText == null) return;

            LiquidSO pouring = _manager?.CurrentPouringLiquid;
            if (pouring == null)
            {
                _currentLiquidVolumeText.gameObject.SetActive(false);
                return;
            }

            float total = 0f;
            foreach (var seg in data.liquidSegments)
            {
                if (seg.liquid == pouring)
                {
                    total = seg.amountMl;
                    break;
                }
            }

            _currentLiquidVolumeText.gameObject.SetActive(true);
            _currentLiquidVolumeText.text = Mathf.FloorToInt(total).ToString() + "ml";

            // 同步更新对应液段图标上的 tooltip 描述
            if (_liquidTooltipTriggers.TryGetValue(pouring, out var trigger))
                trigger.UpdateDescription($"{Mathf.FloorToInt(total)}ml");
        }

    }
}
