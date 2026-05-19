using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using InnsmouthCafe.Data;
using InnsmouthCafe.Managers;

namespace InnsmouthCafe.UI
{
    /// <summary>
    /// 杯型选择UI组件
    /// 用于在制作界面1中选择杯子
    /// </summary>
    public class CupSelectorUI : MonoBehaviour
    {
        [Header("杯型配置")]
        [SerializeField] [Tooltip("可选的杯型数据列表")]
        private List<CupContainerData> _cupDataList = new List<CupContainerData>();

        [Header("UI引用")]
        [SerializeField] [Tooltip("杯型按钮列表")]
        private List<Button> _cupButtons = new List<Button>();

        [SerializeField] [Tooltip("容量文本列表（与按钮对应）")]
        private List<TextMeshProUGUI> _capacityTexts = new List<TextMeshProUGUI>();

        [Header("视觉配置")]
        [SerializeField] [Tooltip("选中状态颜色")]
        private Color _selectedColor = new Color(0.2f, 0.8f, 0.2f); // 绿色

        [SerializeField] [Tooltip("未选中状态颜色")]
        private Color _normalColor = Color.white;

        [SerializeField] [Tooltip("不可选状态颜色")]
        private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f); // 灰色

        private CoffeeCraftManager _manager;
        private int _selectedIndex = -1;

        private void Awake()
        {
            _manager = CoffeeCraftManager.Instance;

            // 初始化按钮事件
            for (int i = 0; i < _cupButtons.Count; i++)
            {
                int index = i; // 闭包捕获
                _cupButtons[i].onClick.AddListener(() => OnCupButtonClick(index));
            }

            // 初始化容量文本
            RefreshCapacityTexts();
        }

        private void Start()
        {
            if (_manager != null)
            {
                _manager.OnCoffeeDataChanged += OnCoffeeDataChanged;
            }

            RefreshDisplay();
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
        private void OnCoffeeDataChanged(CoffeeData coffeeData)
        {
            RefreshDisplay();
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void RefreshDisplay()
        {
            if (_manager == null || _manager.CurrentCoffeeData == null)
            {
                return;
            }

            // 更新选中状态
            UpdateSelectedCup();

            // 更新按钮状态（是否可选）
            UpdateButtonStates();
        }

        /// <summary>
        /// 更新选中的杯子
        /// </summary>
        private void UpdateSelectedCup()
        {
            var currentCup = _manager.CurrentCoffeeData.selectedCup;

            // 查找当前选中的杯子索引
            _selectedIndex = -1;
            if (currentCup != null)
            {
                for (int i = 0; i < _cupDataList.Count; i++)
                {
                    if (_cupDataList[i].cupId == currentCup.cupId)
                    {
                        _selectedIndex = i;
                        break;
                    }
                }
            }

            // 更新按钮颜色
            HighlightSelectedCup();
        }

        /// <summary>
        /// 高亮选中的杯子
        /// </summary>
        private void HighlightSelectedCup()
        {
            for (int i = 0; i < _cupButtons.Count; i++)
            {
                if (i >= _cupDataList.Count)
                {
                    continue;
                }

                var buttonImage = _cupButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    if (i == _selectedIndex)
                    {
                        buttonImage.color = _selectedColor;
                    }
                    else
                    {
                        buttonImage.color = _normalColor;
                    }
                }
            }
        }

        /// <summary>
        /// 更新按钮状态（可选/不可选）
        /// </summary>
        private void UpdateButtonStates()
        {
            bool canSelect = _manager.CanSelectCup();

            for (int i = 0; i < _cupButtons.Count; i++)
            {
                if (i >= _cupDataList.Count)
                {
                    _cupButtons[i].interactable = false;
                    continue;
                }

                _cupButtons[i].interactable = canSelect;

                // 如果不可选，显示灰色
                if (!canSelect && i != _selectedIndex)
                {
                    var buttonImage = _cupButtons[i].GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = _disabledColor;
                    }
                }
            }
        }

        /// <summary>
        /// 杯型按钮点击
        /// </summary>
        private void OnCupButtonClick(int index)
        {
            if (index < 0 || index >= _cupDataList.Count)
            {
                Debug.LogWarning($"[CupSelectorUI] 无效的杯型索引: {index}");
                return;
            }

            if (!_manager.CanSelectCup())
            {
                Debug.LogWarning("[CupSelectorUI] 当前不能切换杯子");
                return;
            }

            var cupData = _cupDataList[index];
            _manager.SelectCup(cupData);

            Debug.Log($"[CupSelectorUI] 选择杯型: {cupData.cupName} ({cupData.capacity}ml)");
        }

        /// <summary>
        /// 刷新容量文本
        /// </summary>
        private void RefreshCapacityTexts()
        {
            for (int i = 0; i < _capacityTexts.Count && i < _cupDataList.Count; i++)
            {
                if (_capacityTexts[i] != null)
                {
                    _capacityTexts[i].text = $"{_cupDataList[i].capacity:F0}ml";
                }
            }
        }

        /// <summary>
        /// 设置杯型数据列表（运行时动态设置）
        /// </summary>
        public void SetCupDataList(List<CupContainerData> cupDataList)
        {
            _cupDataList = cupDataList;
            RefreshCapacityTexts();
            RefreshDisplay();
        }

        /// <summary>
        /// 添加杯型数据
        /// </summary>
        public void AddCupData(CupContainerData cupData)
        {
            if (!_cupDataList.Contains(cupData))
            {
                _cupDataList.Add(cupData);
                RefreshCapacityTexts();
                RefreshDisplay();
            }
        }

        /// <summary>
        /// 手动刷新（用于测试）
        /// </summary>
        public void ManualRefresh()
        {
            RefreshDisplay();
        }
    }
}
